using System.Windows.Forms;
using System.Drawing;
using System;
using CellReport.Core;

namespace CellReport.Presenters.WinForms
{
    public class DisplayPresenter: BaseDisplayPresenter
    {
        private Control _control = null;
        private PresentControl _presentControl = null;

        public DisplayPresenter(CellReport.Core.CellManager manager, Control control)
            : base(manager)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");
            _presentControl = new PresentControl(this) { Dock = DockStyle.Fill };

            _control = control;
            using (var g = _presentControl.CreateGraphics())
            {
                DeviceDpiX = g.DpiX;
                DeviceDpiY = g.DpiY;
                CurrentUnit = CellReport.Core.GraphicsUnit.Pixel;
            }
            Renderer = new GdiPlusRenderer(this);
            if (control != null)
                _control.Controls.Add(_presentControl);
            manager.RowsCountChanged += new EventHandler(RowsAndColumnsCountChanged);
            manager.ColumnsCountChanged += new EventHandler(RowsAndColumnsCountChanged);
            manager.RowHeightChanged += new EventHandler<CellReport.Core.CellManager.RowHeightChangeEventArgs>(RowHeightChanged);
            manager.ColumnWidthChanged += new EventHandler<CellReport.Core.CellManager.ColumnWidthChangeEventArgs>(ColumnWidthChanged);
        }

        private void ColumnWidthChanged(object sender, CellReport.Core.CellManager.ColumnWidthChangeEventArgs e)
        {
            Update();
        }

        private void RowHeightChanged(object sender, CellReport.Core.CellManager.RowHeightChangeEventArgs e)
        {
            Update();
        }

        private void RowsAndColumnsCountChanged(object sender, EventArgs e)
        {
            _presentControl.Update(false);
            Update();
        }

        internal void Control_Paint(object sender, PaintEventArgs e)
        {
            var control = sender as Control;
            var bounds = control.Bounds.ToCellRecttangle();
            RenderFrozenCells(e.Graphics, bounds);
            var x = GetLeftOffset(LeftNoFrozenColumn);
            var y = GetTopOffset(TopNoFrozenRow);
            var clip = new RectangleF(x, y, bounds.Right - x, bounds.Bottom - y);
            var oldClip = e.Graphics.ClipBounds;
            e.Graphics.SetClip(clip);
            RenderCellsRange(e.Graphics, bounds, TopNoFrozenRow, Manager.RowsCount, LeftNoFrozenColumn, Manager.ColumnsCount);
            if (IsCurreCellInFrozen())
            {
                e.Graphics.SetClip(oldClip);
                RenderCurrentCell(e.Graphics);
                //((DisplayRenderer)Renderer).DrawCurrentCell(e.Graphics, bounds.ToRectangleF());
            }
            else
            {
                RenderCurrentCell(e.Graphics);
                //((DisplayRenderer)Renderer).DrawCurrentCell(e.Graphics, bounds.ToRectangleF());
                e.Graphics.SetClip(oldClip);
            }
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

        protected override void OnDblClick(object sender, EventArgs e)
        {
            base.OnDblClick(sender, e);
        }

        protected override void OnProcessKey(object sender, EventArgs e)
        {
            var args = (KeyEventArgs)e;
            if (!args.Alt && !args.Control && !args.Shift)
            {
                switch (args.KeyCode)
                {
                    case Keys.Up: MoveCursorUp(); break;
                    case Keys.Down: MoveCursorDown(); break;
                    case Keys.Left: MoveCursorLeft(); break;
                    case Keys.Right: MoveCursotRight(); break;
                    case Keys.PageUp: MovePageUp(); break;
                    case Keys.PageDown: MovePageDown(); break;
                    case Keys.Home: MoveToFirstColumn(); break;
                    case Keys.End: MoveToLastColumn(); break;
                }
            }
            if (!args.Alt && args.Control && !args.Shift)
            {
                switch (args.KeyCode)
                {
                    case Keys.Home: MoveToFirstRow(); break;
                    case Keys.End: MoveToLastRow(); break;
                }
            }
        }

        public void Update()
        {
            if (_control != null)
                _control.Refresh();
        }

        public Control Control
        {
            get { return _control; }
        }

        protected override CellReport.Core.Size GetTextSize(string text, string fontFamily, float fontSize)
        {
            return GdiPlusRenderer.GetTextSize(text, fontFamily, fontSize);
        }

        protected override CellReport.Core.Rectangle GetHostBounds()
        {
            return _presentControl.GetPaintPanelBounds().ToRecttangle();
        }
    }
}
