using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Styling;
using Avalonia.Threading;
using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CellReport.Avalonia
{
	public class CellControl : Control
	{
		public static readonly StyledProperty<ContentInversionMode> InvertContentModeProperty =
			AvaloniaProperty.Register<CellControl, ContentInversionMode>(
				nameof(InvertContentMode),
				ContentInversionMode.Auto);

		private readonly DispatcherTimer _autoScrollTimer;
		private readonly Dictionary<int, global::Avalonia.Point> _activePointers = new();
		private global::Avalonia.Point? _lastPointerPosition;

		private bool _multiTouchGestureActive;
		private double _pinchStartDistance;
		private float _pinchStartScale;
		private global::Avalonia.Point _lastPanCenter;
		private float _gestureShiftX;
		private float _gestureShiftY;
		private DateTime _twoFingerPressTime;
		private double _twoFingerMaxMove;

		private bool _middleButtonPanActive;
		private global::Avalonia.Point _middleButtonLastPosition;
		private AvaloniaDisplayPresenter? _presenter;

		public AvaloniaDisplayPresenter? Presenter
		{
			get => _presenter;
			set
			{
			_presenter = value;
			ApplyInversionToPresenter();
			InvalidateVisual();
		}
		}

		public ContentInversionMode InvertContentMode
		{
			get => GetValue(InvertContentModeProperty);
			set => SetValue(InvertContentModeProperty, value);
		}

		static CellControl()
		{
			AffectsRender<CellControl>(InvertContentModeProperty);
		}

		public CellControl()
		{
			ClipToBounds = true;
			Focusable = true;

			ActualThemeVariantChanged += OnActualThemeVariantChanged;

			_autoScrollTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(100)
			};
			_autoScrollTimer.Tick += OnAutoScrollTick;
		}

		protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property == InvertContentModeProperty)
				ApplyInversionToPresenter();
		}

		private void OnActualThemeVariantChanged(object? sender, EventArgs e)
		{
			ApplyInversionToPresenter();
		}

		private Color ReportBackground()
		{
			switch (InvertContentMode)
			{
				case ContentInversionMode.Never:
					return Colors.White;
				case ContentInversionMode.Always:
					return Colors.Black;
				default:
					if (this.TryFindResource("CellReport.Grid.Background", ActualThemeVariant, out var brush)
						&& brush is ISolidColorBrush solid)
						return solid.Color;

					return ActualThemeVariant == ThemeVariant.Dark ? Colors.Black : Colors.White;
			}
		}

		private void ApplyInversionToPresenter()
		{
			if (_presenter == null)
				return;

			bool invert;
			switch (InvertContentMode)
			{
				case ContentInversionMode.Always:
					invert = true;
					break;
				case ContentInversionMode.Never:
					invert = false;
					break;
				default:
					invert = ActualThemeVariant == ThemeVariant.Dark;
					break;
			}

			_presenter.InvertColors = invert;
		}

		public override void Render(DrawingContext context)
		{
			var background = ReportBackground();
			context.FillRectangle(new SolidColorBrush(background), new Rect(0, 0, Bounds.Width, Bounds.Height));

			var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
			context.Custom(new CustomDrawOp(bounds, this, Presenter, background.ToSKColor()));
		}

		protected override void OnSizeChanged(SizeChangedEventArgs e)
		{
			base.OnSizeChanged(e);
			if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
				InvalidateVisual();
		}

		protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
		{
			base.OnPointerWheelChanged(e);

			if (Presenter == null)
				return;

			var delta = e.Delta.Y;
			if (Math.Abs(delta) < double.Epsilon)
				return;

			if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
			{
				var factor = delta > 0 ? 1.1f : 0.9f;
				Presenter.Scale = Math.Clamp(Presenter.Scale * factor, 0.25f, 5f);
				Presenter.Update();
				e.Handled = true;
				return;
			}

			var lines = (int)Math.Round(delta);
			Presenter.TopRow = Presenter.TopRow - lines;
			e.Handled = true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (Presenter != null && Presenter.RaiseProcessKey(this, e))
				e.Handled = true;

			base.OnKeyDown(e);
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);

			if (Presenter == null)
				return;

			var point = e.GetCurrentPoint(this);
			var position = e.GetPosition(this);
			_activePointers[e.Pointer.Id] = position;

			if (point.Properties.IsMiddleButtonPressed)
			{
				_middleButtonPanActive = true;
				_middleButtonLastPosition = position;
				_gestureShiftX = 0;
				_gestureShiftY = 0;
				e.Pointer.Capture(this);
				Focus();
				CancelSelection();
				e.Handled = true;
				return;
			}

			if (_activePointers.Count == 2)
			{
				BeginMultiTouchGesture();
				e.Pointer.Capture(this);
				e.Handled = true;
				return;
			}

			if (!point.Properties.IsLeftButtonPressed)
				return;

			e.Pointer.Capture(this);
			Focus();

			var x = Convert.ToSingle(position.X);
			var y = Convert.ToSingle(position.Y);
			_lastPointerPosition = position;

			if (e.ClickCount == 2)
			{
				CancelSelection();
				e.Pointer.Capture(null);
				Presenter.RaiseDblClick(this, e, x, y);
				return;
			}

			Presenter.RaiseClick(this, e, x, y);
			Presenter.StartSelection = Presenter.GetCellByDevicePoint(x, y, false);
			Presenter.IsSelectionMode = true;
			_autoScrollTimer.Start();
			Presenter.Update();
		}

		protected override void OnPointerReleased(PointerReleasedEventArgs e)
		{
			base.OnPointerReleased(e);

			if (Presenter == null)
				return;

			var wasMultiTouch = _activePointers.Count >= 2;
			_activePointers.Remove(e.Pointer.Id);

			if (e.InitialPressMouseButton == MouseButton.Middle && _middleButtonPanActive)
			{
				_middleButtonPanActive = false;
				e.Pointer.Capture(null);
				e.Handled = true;
				return;
			}

			if (wasMultiTouch)
			{
				if (_activePointers.Count < 2)
				{
					if (_twoFingerMaxMove < 12
						&& (DateTime.UtcNow - _twoFingerPressTime).TotalMilliseconds < 350)
					{
						Presenter.Scale = 1f;
						Presenter.Update();
					}

					_multiTouchGestureActive = false;
					_pinchStartDistance = 0;
					_gestureShiftX = 0;
					_gestureShiftY = 0;
				}

				e.Pointer.Capture(null);
				e.Handled = true;
				return;
			}

			if (e.InitialPressMouseButton != MouseButton.Left)
				return;

			if (Presenter.StartSelection.Equals(Presenter.EndSelection))
				Presenter.ClearSelection();
			else if (Presenter.EndSelection.Equals(CellLocation.None))
				Presenter.ClearSelection();

			CancelSelection();
			e.Pointer.Capture(null);
			Presenter.Update();
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);

			if (Presenter == null)
				return;

			var position = e.GetPosition(this);

			if (_activePointers.ContainsKey(e.Pointer.Id))
				_activePointers[e.Pointer.Id] = position;

			if (_middleButtonPanActive && e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
			{
				ApplyPan(
					Convert.ToSingle(position.X - _middleButtonLastPosition.X),
					Convert.ToSingle(position.Y - _middleButtonLastPosition.Y));
				_middleButtonLastPosition = position;
				e.Handled = true;
				return;
			}

			if (_multiTouchGestureActive && _activePointers.Count >= 2)
			{
				UpdateMultiTouchGesture();
				e.Handled = true;
				return;
			}

			if (!Presenter.IsSelectionMode || e.GetCurrentPoint(this).Properties.IsLeftButtonPressed != true)
				return;

			_lastPointerPosition = position;
			Presenter.EndSelection = Presenter.GetCellByDevicePoint(
				Convert.ToSingle(position.X),
				Convert.ToSingle(position.Y),
				false);
			Presenter.Update();
		}

		private void BeginMultiTouchGesture()
		{
			CancelSelection();

			var points = _activePointers.Values.ToArray();
			if (points.Length < 2)
				return;

			_multiTouchGestureActive = true;
			_pinchStartDistance = GetDistance(points[0], points[1]);
			_pinchStartScale = Presenter!.Scale;
			_lastPanCenter = GetCenter(points[0], points[1]);
			_gestureShiftX = 0;
			_gestureShiftY = 0;
			_twoFingerPressTime = DateTime.UtcNow;
			_twoFingerMaxMove = 0;
		}

		private void UpdateMultiTouchGesture()
		{
			if (Presenter == null)
				return;

			var points = _activePointers.Values.ToArray();
			if (points.Length < 2)
				return;

			var distance = GetDistance(points[0], points[1]);
			var center = GetCenter(points[0], points[1]);

			_twoFingerMaxMove = Math.Max(_twoFingerMaxMove, GetDistance(center, _lastPanCenter));

			if (_pinchStartDistance > 10)
			{
				var alfa = distance - _pinchStartDistance;
				const float step = 200f;
				Presenter.Scale = Math.Clamp(_pinchStartScale + (float)(alfa / step), 0.25f, 5f);
			}

			ApplyPan(
				Convert.ToSingle(center.X - _lastPanCenter.X),
				Convert.ToSingle(center.Y - _lastPanCenter.Y));
			_lastPanCenter = center;
		}

		private void ApplyPan(float dx, float dy)
		{
			if (Presenter == null || (Math.Abs(dx) < float.Epsilon && Math.Abs(dy) < float.Epsilon))
				return;

			_gestureShiftY += dy;
			var topRow = Presenter.TopRow;
			while (topRow >= 0
				&& topRow < Presenter.Manager.RowsCount
				&& Math.Abs(_gestureShiftY) > Presenter.GetRowHeight(topRow))
			{
				var shift = _gestureShiftY > 0 ? 1 : -1;
				_gestureShiftY -= Convert.ToInt32(Presenter.GetRowHeight(topRow)) * shift;
				topRow -= shift;
			}
			Presenter.TopRow = topRow;

			_gestureShiftX += dx;
			var leftColumn = Presenter.LeftColumn;
			while (leftColumn >= 0
				&& leftColumn < Presenter.Manager.ColumnsCount
				&& Math.Abs(_gestureShiftX) > Presenter.GetColumnWidth(leftColumn))
			{
				var shift = _gestureShiftX > 0 ? 1 : -1;
				_gestureShiftX -= Convert.ToInt32(Presenter.GetColumnWidth(leftColumn)) * shift;
				leftColumn -= shift;
			}
			Presenter.LeftColumn = leftColumn;
			Presenter.Update();
		}

		private void CancelSelection()
		{
			if (Presenter == null)
				return;

			Presenter.IsSelectionMode = false;
			_lastPointerPosition = null;
			_autoScrollTimer.Stop();
		}

		private static double GetDistance(global::Avalonia.Point a, global::Avalonia.Point b)
		{
			var dx = a.X - b.X;
			var dy = a.Y - b.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		private static global::Avalonia.Point GetCenter(global::Avalonia.Point a, global::Avalonia.Point b)
		{
			return new global::Avalonia.Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
		}

		private void OnAutoScrollTick(object? sender, EventArgs e)
		{
			if (Presenter == null || !_lastPointerPosition.HasValue || Bounds.Width <= 0 || Bounds.Height <= 0)
				return;

			var position = _lastPointerPosition.Value;
			var changed = false;

			if (position.Y > Bounds.Height)
			{
				Presenter.TopRow++;
				changed = true;
			}
			else if (position.Y < 0)
			{
				Presenter.TopRow--;
				changed = true;
			}

			if (position.X > Bounds.Width)
			{
				Presenter.LeftColumn++;
				changed = true;
			}
			else if (position.X < 0)
			{
				Presenter.LeftColumn--;
				changed = true;
			}

			if (!changed)
				return;

			Presenter.EndSelection = Presenter.GetCellByDevicePoint(
				Convert.ToSingle(position.X),
				Convert.ToSingle(position.Y),
				false);
			Presenter.Update();
		}
	}
}
