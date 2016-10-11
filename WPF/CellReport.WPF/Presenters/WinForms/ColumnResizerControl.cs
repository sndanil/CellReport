using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace CellReport.Presenters.WinForms
{
    internal partial class ColumnResizerControl : DoubleBufferedControl
    {
        public ColumnResizerControl()
        {
            InitializeComponent();

            TabStop = false;
            this.SetStyle(ControlStyles.Selectable, true);
            this.ForeColor = Color.FromArgb(39, 65, 62);
        }

        internal DisplayPresenter Presenter
        {
            get;
            set;
        }

        private void DrawCellBackground(Graphics g, float left, float right, int col)
        {
            var rect = new RectangleF(left, 0, right - left, this.Height);
            if (rect.Height != 0 && rect.Width != 0)
            {
                var cell = Presenter.Manager.GetCell(Presenter.CurrentCell);
                if (col == Presenter.CurrentCell.Column || (cell != null && col >= cell.Location.Column && col <= cell.Location.Column + cell.Span.ColSpan))
                {
                    using (var brush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(249, 217, 159),
                        Color.FromArgb(241, 193, 95), 90f))
                        g.FillRectangle(brush, rect);
                }
            }
        }

        private void DrawCell(Graphics g, float left, float right, string text)
        {
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            var textHeight = g.MeasureString(text, this.Font).Height;
            var rect = new RectangleF(left, (this.Height - textHeight) / 2, right - left, textHeight);
            if (rect.Height != 0 && rect.Width != 0)
            {
                using (var brush = new SolidBrush(ForeColor))
                    g.DrawString(text, this.Font, brush, rect, stringFormat);
                using (var pen = new Pen(Color.FromArgb(158, 182, 206), 1))
                    g.DrawLine(pen, right, 0, right, this.Height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(new Point(0, 0), new Point(0, this.Height), Color.White, Color.FromArgb(211, 219, 233)))
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            if (Presenter != null)
            {
                foreach (var column in GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(column);
                    var right = left + Presenter.GetColumnWidth(column);

                    DrawCellBackground(e.Graphics, left, right, column);
                }
                foreach (var column in GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(column);
                    var right = left + Presenter.GetColumnWidth(column);

                    DrawCell(e.Graphics, left, right, (column + 1).ToString());
                }
            }
            using (var pen = new Pen(Color.FromArgb(158, 182, 206), 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, this.Width - 1, 0);
                e.Graphics.DrawLine(pen, 0, this.Height - 1, this.Width - 1, this.Height - 1);
            }
        }

        private IEnumerable<int> GetColumnIndexes()
        {
            if (Presenter != null)
            {
                foreach (var colIndex in Presenter.GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(colIndex);
                    if (left > this.Width)
                        break;

                    yield return colIndex;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Presenter != null)
            {
                if (_resizeColumn < 0)
                {
                    this.Cursor = Cursors.Default;
                    foreach (var column in GetColumnIndexes())
                    {
                        var left = Presenter.GetLeftOffset(column);
                        var right = left + Presenter.GetColumnWidth(column);

                        if (e.X <= right + 3 && e.X >= right - 3)
                            this.Cursor = Cursors.VSplit;
                    }
                }
                else if (_resizeColumn >= 0 && _lastX != e.X)
                {
                    var oldWidth = Presenter.GetColumnWidth(_resizeColumn);
                    var width = oldWidth + (e.X - _lastX);
                    Presenter.SetColumnWidth(_resizeColumn, width);
                }
                _lastX = e.X;
            }
        }

        private int _resizeColumn = -1;
        private int _lastX = 0;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (this.Cursor == Cursors.VSplit && e.Button == MouseButtons.Left)
            {
                _resizeColumn = -1;
                foreach (var column in GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(column);
                    var right = left + Presenter.GetColumnWidth(column);

                    if (e.X <= right + 3 && e.X >= right - 3)
                        _resizeColumn = column;
                }
                if (_resizeColumn >= 0)
                    this.Capture = true;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _resizeColumn = -1;
            this.Capture = false; ;
        }
    }
}
