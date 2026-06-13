using Avalonia.Controls;
using Avalonia.Input;
using CellReport.Core;
using CellReport.Presenters;
using SkiaSharp;
using System;

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
				_presentControl.Invalidate();
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

		public override void Render(object sender, object context)
		{
			if (context is not SKCanvas canvas)
				return;

			_replaceClipActive = false;
			BeginRender();

			var control = sender as Control;
			var bounds = control.Bounds.ToRecttangle();
			RenderFrozenCells(context, bounds);

			var x = GetLeftOffset(LeftNoFrozenColumn);
			var y = GetTopOffset(TopNoFrozenRow);
			var clip = new Rectangle(x, y, bounds.Right - x, bounds.Bottom - y);

			SetClip(context, clip);
			RenderCellsRange(context, bounds, TopNoFrozenRow, Manager.RowsCount, LeftNoFrozenColumn, Manager.ColumnsCount);
			ResetClipState(canvas);

			RenderCurrentCell(context, bounds);
			RenderSelection(context);
			EndRender();
		}
	}
}
