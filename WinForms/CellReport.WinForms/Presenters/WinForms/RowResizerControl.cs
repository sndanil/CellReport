using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CellReport.Presenters.WinForms
{
    public partial class RowResizerControl : DoubleBufferedControl
    {
        public RowResizerControl()
        {
            InitializeComponent();

            TabStop = false;
            this.SetStyle(ControlStyles.Selectable, true);
            this.ForeColor = Color.FromArgb(39, 65, 62);
        }

        public BaseDisplayPresenter Presenter
        {
            get;
            set;
        }

        public int TopOffset
        {
            get;
            set;
        }

        private void DrawCellBackground(Graphics g, float top, float bottom, int row)
        {
            var rect = new RectangleF(0, top, this.Width, bottom - top);
            if (rect.Height != 0 && rect.Width != 0)
            {
                var cell = Presenter.Manager.GetCell(Presenter.CurrentCell);
                if (row == Presenter.CurrentCell.Row || (cell != null && row >= cell.Location.Row && row <= cell.Location.Row + cell.Span.RowSpan))
                {
                    using (var brush = new LinearGradientBrush(rect, Color.FromArgb(0xFF, 0xD3, 0x59), Color.FromArgb(0xFF, 0xEF, 0x71), 90f))
                        g.FillRectangle(brush, rect);
                }
            }
        }

        private void DrawCell(Graphics g, float top, float bottom, int row)
        {
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Far;
            var rect = new RectangleF(0, top, this.Width, bottom - top);
            if (rect.Height != 0 && rect.Width != 0)
            {
                using (var brush = new SolidBrush(Color.FromArgb(0x36, 0x36, 0x36)))
                using (var pen = new Pen(Color.FromArgb(0xB1, 0xB5, 0xBA)))
                {
                    g.DrawString((row + 1).ToString(), this.Font, brush, rect, stringFormat);
                    g.DrawLine(pen, 0, bottom, this.Width, bottom);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(0xDF, 0xE3, 0xE8)))
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            if (Presenter != null)
            {
				e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using (var pen = new Pen(Color.FromArgb(158, 182, 206), 1))
                    e.Graphics.DrawLine(pen, 0, TopOffset - 1, this.Width, TopOffset - 1);
                foreach (var row in GetRowIndexes())
                {
                    var top = Presenter.GetTopOffset(row) + TopOffset;
                    var bottom = top + Presenter.GetRowHeight(row);

                    DrawCellBackground(e.Graphics, top, bottom, row);
                }
                foreach (var row in GetRowIndexes())
                {
                    var top = Presenter.GetTopOffset(row) + TopOffset;
                    var bottom = top + Presenter.GetRowHeight(row);

                    DrawCell(e.Graphics, top, bottom, row);
                }
            }
            using (var pen = new Pen(Color.FromArgb(0xB1, 0xB5, 0xBA)))
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        }

        private IEnumerable<int> GetRowIndexes()
        {
            if (Presenter != null)
            {
                foreach (var rowIndex in Presenter.GetRowIndexes())
                {
                    var top = Presenter.GetTopOffset(rowIndex) + TopOffset;
                    if (top > this.Height)
                        break;

                    yield return rowIndex;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Presenter != null)
            {
				if (_resizeRow < 0 && _startSelectionRow < 0)
                {
                    this.Cursor = Cursors.Default;
                    foreach (var row in GetRowIndexes())
                    {
                        var top = Presenter.GetTopOffset(row) + TopOffset;
                        var bottom = top + Presenter.GetRowHeight(row);

                        if (e.Y <= bottom + 3 && e.Y >= bottom - 3)
                            this.Cursor = Cursors.SizeNS;
                    }
                }
                else if (_resizeRow >= 0 && _lastY != e.Y)
                {
                    var oldHeight = Presenter.GetRowHeight(_resizeRow);
                    var height = oldHeight + (e.Y - _lastY);
                    Presenter.SetRowHeight(_resizeRow, height);
					this.Update();
                }
				else if (_startSelectionRow >= 0 && _lastY != e.Y)
				{
					var findRow = -1;
					foreach (var row in GetRowIndexes())
					{
						var top = Presenter.GetTopOffset(row) + TopOffset;
						if (top <= e.Y && e.Y <= top + Presenter.GetRowHeight(row))
						{
							findRow = row;
							break;
						}
					}
					if (findRow >= 0)
					{
						Presenter.BeginUpdate();
						Presenter.StartSelection = new Core.CellLocation() { Row = _startSelectionRow, Column = 0 };
						Presenter.EndSelection = new Core.CellLocation() { Row = findRow, Column = Presenter.Manager.ColumnsCount - 1 };
						Presenter.EndUpdate();
					}
				}
                _lastY = e.Y;
            }
        }

        private int _resizeRow = -1;
		private int _startSelectionRow = -1;
        private int _lastY = 0;

        protected override void OnMouseDown(MouseEventArgs e)
        {
			if (e.Button != MouseButtons.Left)
				return;

            if (this.Cursor == Cursors.SizeNS)
            {
                _resizeRow = -1;
                foreach (var row in GetRowIndexes())
                {
                    var top = Presenter.GetTopOffset(row) + TopOffset;
                    var bottom = top + Presenter.GetRowHeight(row);

                    if (e.Y <= bottom + 3 && e.Y >= bottom - 3)
                        _resizeRow = row;
                }
				if (_resizeRow >= 0)
				{
					this.Capture = true;
					Presenter.State = PresenterState.RowResize;
				}
            }
			else
			{
				_startSelectionRow = -1;
				foreach (var row in GetRowIndexes())
				{
					var top = Presenter.GetTopOffset(row) + TopOffset;
					if (top <= e.Y && e.Y <= top + Presenter.GetRowHeight(row))
					{
						_startSelectionRow = row;
						break;
					}
				}
				if (_startSelectionRow >= 0)
				{
					Presenter.BeginUpdate();
					Presenter.StartSelection = new Core.CellLocation() { Row = _startSelectionRow, Column = 0 };
					Presenter.EndSelection = new Core.CellLocation() { Row = _startSelectionRow, Column = Presenter.Manager.ColumnsCount - 1 };
					Presenter.EndUpdate();
					this.Capture = true;
				}
			}
		}

        protected override void OnMouseUp(MouseEventArgs e)
        {
			if (_resizeRow >= 0)
			{
				_resizeRow = -1;
				Presenter.State = PresenterState.Normal;
			}
			_startSelectionRow = -1;
            this.Capture = false;
        }
    }
}
