using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CellReport.Core;
using System;

namespace CellReport.Avalonia
{
	public class CellControl : Control
	{
		private readonly DispatcherTimer _autoScrollTimer;
		private global::Avalonia.Point? _lastPointerPosition;

		public AvaloniaDisplayPresenter? Presenter { get; set; }

		public CellControl()
		{
			ClipToBounds = true;
			Focusable = true;

			_autoScrollTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(100)
			};
			_autoScrollTimer.Tick += OnAutoScrollTick;
		}

		public override void Render(DrawingContext context)
		{
			context.FillRectangle(Brushes.White, new Rect(0, 0, Bounds.Width, Bounds.Height));
			context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), this, Presenter));
		}

		protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
		{
			base.OnPointerWheelChanged(e);

			if (Presenter == null)
				return;

			var delta = e.Delta.Y;
			if (Math.Abs(delta) < double.Epsilon)
				return;

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

			if (Presenter == null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
				return;

			e.Pointer.Capture(this);
			Focus();

			var position = e.GetPosition(this);
			var x = Convert.ToSingle(position.X);
			var y = Convert.ToSingle(position.Y);
			_lastPointerPosition = position;

			if (e.ClickCount == 2)
			{
				Presenter.IsSelectionMode = false;
				_autoScrollTimer.Stop();
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

			if (Presenter == null || e.InitialPressMouseButton != MouseButton.Left)
				return;

			if (Presenter.StartSelection.Equals(Presenter.EndSelection))
				Presenter.ClearSelection();
			else if (Presenter.EndSelection.Equals(CellLocation.None))
				Presenter.ClearSelection();

			Presenter.IsSelectionMode = false;
			_lastPointerPosition = null;
			_autoScrollTimer.Stop();
			e.Pointer.Capture(null);
			Presenter.Update();
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);

			if (Presenter == null || !Presenter.IsSelectionMode || e.GetCurrentPoint(this).Properties.IsLeftButtonPressed != true)
				return;

			var position = e.GetPosition(this);
			_lastPointerPosition = position;
			Presenter.EndSelection = Presenter.GetCellByDevicePoint(
				Convert.ToSingle(position.X),
				Convert.ToSingle(position.Y),
				false);
			Presenter.Update();
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
