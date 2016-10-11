using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace CellReport.Presenters.Wpf
{
    public class ColumnResizerControl: Control
    {
        private SolidColorBrush _lineBrush = new SolidColorBrush(Color.FromRgb(0xB1, 0xB5, 0xBA));
        private SolidColorBrush _backBrush = new SolidColorBrush(Color.FromRgb(0xDF, 0xE3, 0xE8));
        private SolidColorBrush _textBrush = new SolidColorBrush(Color.FromRgb(0x36, 0x36, 0x36));
        
        public ColumnResizerControl()
            : base()
        {
            VisualTextRenderingMode = TextRenderingMode.Aliased;
            this.SnapsToDevicePixels = true;
            this.ClipToBounds = true;
            this.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        public float XOffset
        {
            get;
            set;
        }

        public BaseDisplayPresenter Presenter
        {
            get;
            internal set;
        }

        private void RenderCell(DrawingContext dc, float left, float right, string text)
        {
            var formattedText = new FormattedText(text, System.Threading.Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft Sans Serif"),
                Presenter.ConvertToDeviceX(8, CellReport.Core.GraphicsUnit.Point),
                _textBrush,
                new NumberSubstitution(),
                TextFormattingMode.Ideal);
            formattedText.MaxTextHeight = this.ActualHeight;
            formattedText.MaxTextWidth = (right - left) > 0 ? (right - left) : 0.01;
            formattedText.TextAlignment = TextAlignment.Center;
            var position = new Point(left, (this.ActualHeight - formattedText.Height) / 2);
            dc.DrawText(formattedText, position);
            var pen = new Pen(_lineBrush, 1);
            dc.DrawLine(pen, new Point(right, 0), new Point(right, this.ActualHeight));
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(_backBrush, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            var pen = new Pen(_lineBrush, 1);
            drawingContext.DrawLine(pen, new Point(0, 0), new Point(this.ActualWidth, 0));
            drawingContext.DrawLine(pen, new Point(0, this.ActualHeight), 
                new Point(this.ActualWidth, this.ActualHeight));
            drawingContext.DrawLine(pen, new Point(XOffset, 0), new Point(XOffset, this.ActualHeight));
            if (Presenter != null)
            {
                foreach (var column in GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(column) + XOffset;
                    var right = left + Presenter.GetColumnWidth(column);

                    if (CellReport.Core.CellManager.IsDesignMode)
                        RenderCell(drawingContext, left, right, column.ToString());
                    else
                        RenderCell(drawingContext, left, right, (column + 1).ToString());
                }
            }
        }

        private IEnumerable<int> GetColumnIndexes()
        {
            if (Presenter != null)
            {
                for (int column = 0; column < Presenter.FrozenColumnsCount; column++)
                {
                    var left = Presenter.GetLeftOffset(column);
                    var right = left + Presenter.GetColumnWidth(column);
                    if (right > this.ActualWidth)
                        break;

                    yield return column;
                }
                for (int column = Presenter.LeftNoFrozenColumn; column < Presenter.Manager.ColumnsCount; column++)
                {
                    var left = Presenter.GetLeftOffset(column);
                    var right = left + Presenter.GetColumnWidth(column);
                    if (right > this.ActualWidth)
                        break;

                    yield return column;
                }
            }
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (Presenter != null)
            {
                var x = e.GetPosition(this).X - XOffset;
                if (_resizeColumn < 0 && _startSelectionColumn < 0)
                {
                    this.Cursor = Cursors.Arrow;
                    foreach (var column in GetColumnIndexes())
                    {
                        var left = Presenter.GetLeftOffset(column);
                        var right = left + Presenter.GetColumnWidth(column);

                        if (x <= right + 3 && x >= right - 3)
                            this.Cursor = Cursors.SizeWE;
                    }
                }
                else if (_resizeColumn >= 0 && _lastX != x)
                {
                    var oldWidth = Presenter.GetColumnWidth(_resizeColumn);
                    var width = oldWidth + (x - _lastX);
                    Presenter.SetColumnWidth(_resizeColumn, Convert.ToSingle(width));
                }
                else if (_startSelectionColumn >= 0 && _lastX != x)
                {
                    var findCol = -1;
                    foreach (var col in GetColumnIndexes())
                    {
                        var left = Presenter.GetLeftOffset(col);
                        if (left <= x && x <= left + Presenter.GetColumnWidth(col))
                        {
                            findCol = col;
                            break;
                        }
                    }
                    if (findCol >= 0)
                    {
                        Presenter.BeginUpdate();
                        Presenter.StartSelection = new Core.CellLocation() { Row = 0, Column = _startSelectionColumn };
                        Presenter.EndSelection = new Core.CellLocation() { Row = Presenter.Manager.RowsCount - 1, Column = findCol };
                        Presenter.EndUpdate();
                    }
                }
                _lastX = x;
            }
        }

        private int _resizeColumn = -1;
        private int _startSelectionColumn = -1;
        private double _lastX = 0;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var x = e.GetPosition(this).X - XOffset;
            if (this.Cursor == Cursors.SizeWE)
            {
                _resizeColumn = -1;
                foreach (var column in GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(column);
                    var right = left + Presenter.GetColumnWidth(column);

                    if ((x <= right + 3) && (x >= right - 3) &&
						((left < right) || CellReport.Core.MarkerResolver.Resolve(Presenter.Manager.GetColumnMarker(column))))
                        _resizeColumn = column;
                }
                if (_resizeColumn >= 0)
                    Mouse.Capture(this);
            }
            else
            {
                _startSelectionColumn = -1;
                foreach (var col in GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(col);
                    if (left <= x && x <= left + Presenter.GetColumnWidth(col))
                    {
                        _startSelectionColumn = col;
                        break;
                    }
                }
                if (_startSelectionColumn >= 0)
                {
                    Presenter.BeginUpdate();
                    Presenter.StartSelection = new Core.CellLocation() { Row = 0, Column = _startSelectionColumn };
                    Presenter.EndSelection = new Core.CellLocation() { Row = Presenter.Manager.RowsCount - 1, Column = _startSelectionColumn };
                    Presenter.EndUpdate();
                    Mouse.Capture(this);
                }
            }
        }

        protected override void  OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            _resizeColumn = -1;
            _startSelectionColumn = -1;
            Mouse.Capture(null);
        }
    }
}
