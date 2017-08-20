using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using CellReport.Core;

namespace CellReport.Presenters.WinForms
{
	public sealed partial class ColumnResizerControl : DoubleBufferedControl
	{
		public ColumnResizerControl()
		{
			InitializeComponent();

			TabStop = false;
			SetStyle(ControlStyles.Selectable, true);
			ForeColor = Color.FromArgb(39, 65, 62);
		}

		public BaseDisplayPresenter Presenter
		{
			get;
			set;
		}

		private void DrawCellBackground(Graphics g, float left, float right, int col)
		{
			var rect = new RectangleF(left, 0, right - left, Height);
			if (rect.Height != 0 && rect.Width != 0)
			{
				var cell = Presenter.Manager.GetCell(Presenter.CurrentCell);
				if (col == Presenter.CurrentCell.Column || (cell != null && col >= cell.Location.Column && col <= cell.Location.Column + cell.Span.ColSpan))
				{
					using (var brush = new LinearGradientBrush(rect, Color.FromArgb(0xFF, 0xD3, 0x59), Color.FromArgb(0xFF, 0xEF, 0x71), 90f))
						g.FillRectangle(brush, rect);
				}
			}
		}

		private void DrawCell(Graphics g, float left, float right, string text)
		{
			StringFormat stringFormat = new StringFormat();
			stringFormat.Alignment = StringAlignment.Center;
			stringFormat.LineAlignment = StringAlignment.Center;

			var textHeight = g.MeasureString(text, Font).Height;
			var rect = new RectangleF(left, (Height - textHeight) / 2, right - left, textHeight);
			if (rect.Height != 0 && rect.Width != 0)
			{
				using (var textBrush = new SolidBrush(Color.FromArgb(0x36, 0x36, 0x36)))
				using (var linePen = new Pen(Color.FromArgb(0xB1, 0xB5, 0xBA)))
				{
					g.DrawString(text, Font, textBrush, rect, stringFormat);
					g.DrawLine(linePen, right, 0, right, Height);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			using (var brush = new SolidBrush(Color.FromArgb(0xDF, 0xE3, 0xE8)))
				e.Graphics.FillRectangle(brush, ClientRectangle);
			if (Presenter != null)
			{
				e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
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

					if (CellManager.IsDesignMode)
						DrawCell(e.Graphics, left, right, column.ToString());
					else
						DrawCell(e.Graphics, left, right, (column + 1).ToString());
				}
			}
			using (var pen = new Pen(Color.FromArgb(0xB1, 0xB5, 0xBA)))
			{
				e.Graphics.DrawLine(pen, 0, 0, Width - 1, 0);
				e.Graphics.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);
			}
		}

		private IEnumerable<int> GetColumnIndexes()
		{
			if (Presenter != null)
			{
				foreach (var colIndex in Presenter.GetColumnIndexes())
				{
					var left = Presenter.GetLeftOffset(colIndex);
					if (left > Width)
						break;

					yield return colIndex;
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (Presenter != null)
			{
				if (_resizeColumn < 0 && _startSelectionColumn < 0)
				{
					Cursor = Cursors.Default;
					foreach (var column in GetColumnIndexes())
					{
						var left = Presenter.GetLeftOffset(column);
						var right = left + Presenter.GetColumnWidth(column);

						if (e.X <= right + 3 && e.X >= right - 3)
							Cursor = Cursors.SizeWE;
					}
				}
				else if (_resizeColumn >= 0 && _lastX != e.X)
				{
					var oldWidth = Presenter.GetColumnWidth(_resizeColumn);
					var width = oldWidth + (e.X - _lastX);
					Presenter.SetColumnWidth(_resizeColumn, width);
					Update();
				}
				else if (_startSelectionColumn >= 0 && _lastX != e.X)
				{
					var findCol = -1;
					foreach (var col in GetColumnIndexes())
					{
						var left = Presenter.GetLeftOffset(col);
						if (left <= e.X && e.X <= left + Presenter.GetColumnWidth(col))
						{
							findCol = col;
							break;
						}
					}
					if (findCol >= 0)
					{
						Presenter.BeginUpdate();
						Presenter.StartSelection = new CellLocation() { Row = 0, Column = _startSelectionColumn };
						Presenter.EndSelection = new CellLocation() { Row = Presenter.Manager.RowsCount - 1, Column = findCol };
						Presenter.EndUpdate();
					}
				}
				_lastX = e.X;
			}
		}

		private int _resizeColumn = -1;
		private int _startSelectionColumn = -1;
		private int _lastX = 0;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
				return;

			if (Cursor == Cursors.SizeWE)
			{
				_resizeColumn = -1;
				foreach (var column in GetColumnIndexes())
				{
					var left = Presenter.GetLeftOffset(column);
					var right = left + Presenter.GetColumnWidth(column);

					if ((e.X <= right + 3) && (e.X >= right - 3) &&
						((left < right) || MarkerResolver.Resolve(Presenter.Manager.GetColumnMarker(column))))
						_resizeColumn = column;
				}
				if (_resizeColumn >= 0)
				{
					Capture = true;
					Presenter.State = PresenterState.ColumnResize;
				}
			}
			else
			{
				_startSelectionColumn = -1;
				foreach (var col in GetColumnIndexes())
				{
					var left = Presenter.GetLeftOffset(col);
					if (left <= e.X && e.X <= left + Presenter.GetColumnWidth(col))
					{
						_startSelectionColumn = col;
						break;
					}
				}
				if (_startSelectionColumn >= 0)
				{
					Presenter.BeginUpdate();
					Presenter.StartSelection = new CellLocation() { Row = 0, Column = _startSelectionColumn };
					Presenter.EndSelection = new CellLocation() { Row = Presenter.Manager.RowsCount - 1, Column = _startSelectionColumn };
					Presenter.EndUpdate();
					Capture = true;
				}
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (_resizeColumn >= 0)
			{
				_resizeColumn = -1;
				Presenter.State = PresenterState.Normal;
			}
			_startSelectionColumn = -1;
			Capture = false;
		}
	}
}
