using System.Windows.Forms;
using System.Drawing;
using System;
using CellReport.Core;
using System.Drawing.Text;

namespace CellReport.Presenters.WinForms
{
	public class DisplayPresenter : BaseDisplayPresenter
	{
		private Control _control;
		private PresentControl _presentControl;

		public PresentControl PresentControl { get => _presentControl; }

		public DisplayPresenter(CellManager manager, Control control, string defaultPrinterName = null)
				: base(manager)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));

			_presentControl = new PresentControl(this) { Dock = DockStyle.Fill, DefaultPrinterName = defaultPrinterName };

			_control = control;
			using (var g = _presentControl.CreateGraphics())
			{
				DeviceDpiX = g.DpiX;
				DeviceDpiY = g.DpiY;
				CurrentUnit = Core.GraphicsUnit.Pixel;
			}
			ProcessFit();

			Renderer = new GdiPlusRenderer();
			if (control != null)
				_control.Controls.Add(_presentControl);
		}

		protected override void SetClip(object context, Core.Rectangle clip)
		{
			var g = (Graphics)context;
			g.SetClip(clip.ToRectangleF());
		}

		public override void Render(object sender, object context)
		{
			var e = context as PaintEventArgs;
			this.BeginRender();
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			var oldClip = e.Graphics.ClipBounds;
			var control = sender as Control;
			var bounds = control.Bounds.ToCellRecttangle();
			RenderFrozenCells(e.Graphics, bounds);
			var x = GetLeftOffset(LeftNoFrozenColumn);
			var y = GetTopOffset(TopNoFrozenRow);
			var clip = new RectangleF(x, y, bounds.Right - x, bounds.Bottom - y);
			e.Graphics.SetClip(clip);
			RenderCellsRange(e.Graphics, bounds, TopNoFrozenRow, Manager.RowsCount, LeftNoFrozenColumn, Manager.ColumnsCount);
			if (IsCurreCellInFrozen())
			{
				e.Graphics.SetClip(oldClip);
				RenderCurrentCell(e.Graphics, bounds);
			}
			else
			{
				RenderCurrentCell(e.Graphics, bounds);
				e.Graphics.SetClip(oldClip);
			}
			RenderSelection(e.Graphics);
			this.EndRender();
		}

		public override void RenderPager(object sender, object context, Pager pager)
		{
			var control = sender as Control;
			var e = context as PaintEventArgs;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			Renderer.RenderPager(e.Graphics, pager, control.Bounds.ToCellRecttangle(), Scale);
		}

		protected override void OnClick(object sender, EventArgs e)
		{
			if (e is MouseEventArgs)
			{
				var args = (MouseEventArgs)e;
				var location = GetCellByDevicePoint(args.X, args.Y, false);
				CurrentCell = location;
			}
		}

		protected override bool OnProcessKey(object sender, EventArgs e)
		{
			var args = (KeyEventArgs)e;
			if (!args.Alt && !args.Control && !args.Shift)
			{
				switch (args.KeyCode)
				{
					case Keys.Up: MoveCursorUp(); return true;
					case Keys.Down: MoveCursorDown(); return true;
					case Keys.Left: MoveCursorLeft(); return true;
					case Keys.Right: MoveCursotRight(); return true;
					case Keys.PageUp: MovePageUp(); return true;
					case Keys.PageDown: MovePageDown(); return true;
					case Keys.Home: MoveToFirstColumn(); return true;
					case Keys.End: MoveToLastColumn(); return true;
				}
			}
			if (!args.Alt && args.Control && !args.Shift)
			{
				switch (args.KeyCode)
				{
					case Keys.Home: MoveToFirstRow(); return true;
					case Keys.End: MoveToLastRow(); return true;
					case Keys.C:
					case Keys.Insert:
						var data = Manager.IsDesignMode ? GetCurrentForClipBoard() : GetSelectionForClipboard();
						if (data != null)
						{
							if (data is byte[])
								Clipboard.SetImage(Image.FromStream(new System.IO.MemoryStream((byte[])data)));
							else
								Clipboard.SetDataObject(data);
						}
						return true;
				}
			}

			return false;
		}

		public override void Update()
		{
			if (_control != null && !IsInUpdateMode())
				_presentControl.Update(true);
		}

		public Control Control
		{
			get { return _control; }
			protected set { _control = value; }
		}

		protected override Core.Size GetTextSize(Core.Rectangle rect, string text, string fontFamily, float fontSize, Core.FontStyle fontStyle)
		{
			return GdiPlusRenderer.GetTextSize(rect, text, fontFamily, fontSize, fontStyle);
		}

		protected override Core.Rectangle GetHostBounds()
		{
			return _presentControl.GetPaintPanelBounds().ToRecttangle();
		}
	}
}
