using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using CellReport.Core;
using CellReport.Presenters;
using CellReport.Presenters.Skia;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using AvaloniaInput = Avalonia.Input;

namespace CellReport.Avalonia
{
	public class AvaloniaDisplayPresenter : BaseDisplayPresenter
	{
		private readonly PresentControl _presentControl;
		private readonly CellControl _cellControl;
		private bool _replaceClipActive;

		public AvaloniaDisplayPresenter(CellManager manager, PresentControl presentControl, CellControl cellControl)
				: base(manager)
		{
			_presentControl = presentControl ?? throw new ArgumentNullException(nameof(presentControl));
			_cellControl = cellControl ?? throw new ArgumentNullException(nameof(cellControl));

			Renderer = new AvaloniaRenderer();
			DeviceDpiX = 96;
			DeviceDpiY = 96;
			CurrentUnit = GraphicsUnit.WpfPoint;
			ProcessFit();
		}

		public bool InvertColors
		{
			get => Renderer is SkiaReportRenderer skiaRenderer && skiaRenderer.InvertColors;
			set
			{
				var skiaRenderer = Renderer as SkiaReportRenderer;
				if (skiaRenderer == null || skiaRenderer.InvertColors == value)
					return;

				skiaRenderer.InvertColors = value;
				Update();
			}
		}

		protected override void SetClip(object context, Rectangle clip)
		{
			if (context is not SKCanvas canvas)
				return;

			if (_replaceClipActive)
			{
				canvas.Restore();
				_replaceClipActive = false;
			}

			canvas.Save();
			_replaceClipActive = true;
			canvas.ClipRect(clip.ToRect(), SKClipOperation.Intersect, antialias: false);
		}

		private void ResetClipState(SKCanvas canvas)
		{
			if (_replaceClipActive)
			{
				canvas.Restore();
				_replaceClipActive = false;
			}
		}

		protected override Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			return AvaloniaRenderer.GetTextSize(rect, text, fontFamily, fontSize, fontStyle);
		}

		protected override Rectangle GetHostBounds()
		{
			return _cellControl.Bounds.ToRecttangle();
		}

		public override void Update()
		{
			if (!IsInUpdateMode())
				_presentControl.Update();
		}

		protected override void OnClick(object sender, EventArgs e)
		{
			if (e is PointerPressedEventArgs args && sender is Control ctrl)
			{
				var position = args.GetPosition(ctrl);
				var location = GetCellByDevicePoint(Convert.ToSingle(position.X), Convert.ToSingle(position.Y), false);
				CurrentCell = location;
			}
		}

		protected override bool OnProcessKey(object sender, EventArgs e)
		{
			if (e is not KeyEventArgs args)
				return false;

			var key = args.Key;
			var modifiers = args.KeyModifiers;

			if (modifiers == AvaloniaInput.KeyModifiers.None)
			{
				switch (key)
				{
					case Key.Up:
						MoveCursorUp();
						return true;
					case Key.Down:
						MoveCursorDown();
						return true;
					case Key.Left:
						MoveCursorLeft();
						return true;
					case Key.Right:
						MoveCursotRight();
						return true;
					case Key.PageUp:
						MovePageUp();
						return true;
					case Key.PageDown:
						MovePageDown();
						return true;
					case Key.Home:
						MoveToFirstColumn();
						return true;
					case Key.End:
						MoveToLastColumn();
						return true;
				}
			}

			if (modifiers == AvaloniaInput.KeyModifiers.Control)
			{
				switch (key)
				{
					case Key.Home:
						MoveToFirstRow();
						return true;
					case Key.End:
						MoveToLastRow();
						return true;
					case Key.C:
					case Key.Insert:
						if (CellManager.IsDesignMode)
							return false;
						CopySelectionToClipboard();
						return true;
				}
			}

			return false;
		}

		private void CopySelectionToClipboard()
		{
			var data = CellManager.IsDesignMode ? GetCurrentForClipBoard() : GetSelectionForClipboard();
			if (data == null)
				return;

			_ = CopyToClipboardAsync(_cellControl, data);
		}

		private static async Task CopyToClipboardAsync(Control control, object data)
		{
			var clipboard = TopLevel.GetTopLevel(control)?.Clipboard;
			if (clipboard == null)
				return;

			switch (data)
			{
				case string text:
					await clipboard.SetTextAsync(text);
					break;
				case byte[] bytes when bytes.Length > 0:
					using (var stream = new MemoryStream(bytes))
					{
						var bitmap = new Bitmap(stream);
						await clipboard.SetBitmapAsync(bitmap);
					}
					break;
			}
		}

		public override void Render(object sender, object context)
		{
			if (context is not SKCanvas canvas)
				return;

			_replaceClipActive = false;
			BeginRender();

			var control = sender as Control;
			if (control == null)
				return;

			var bounds = control.Bounds.ToRecttangle();
			RenderFrozenCells(context, bounds);

			var x = GetLeftOffset(LeftNoFrozenColumn);
			var y = GetTopOffset(TopNoFrozenRow);
			var clip = new Rectangle(x, y, bounds.Right - x, bounds.Bottom - y);

			SetClip(context, clip);
			RenderCellsRange(context, bounds, TopNoFrozenRow, Manager.RowsCount, LeftNoFrozenColumn, Manager.ColumnsCount);

			if (IsCurreCellInFrozen())
			{
				ResetClipState(canvas);
				RenderCurrentCell(context, bounds);
			}
			else
			{
				RenderCurrentCell(context, bounds);
				ResetClipState(canvas);
			}

			RenderSelection(context);
			EndRender();
		}
	}
}
