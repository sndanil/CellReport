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
    public class RowResizerControl: Control
    {
        private SolidColorBrush _lineBrush = new SolidColorBrush(Color.FromRgb(0xB1, 0xB5, 0xBA));
        private SolidColorBrush _backBrush = new SolidColorBrush(Color.FromRgb(0xDF, 0xE3, 0xE8));
        private SolidColorBrush _textBrush = new SolidColorBrush(Color.FromRgb(0x36, 0x36, 0x36));        

        public RowResizerControl()
            : base()
        {
            VisualTextRenderingMode = TextRenderingMode.Aliased;
            this.SnapsToDevicePixels = true;
            this.ClipToBounds = true;
            this.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        public BaseDisplayPresenter Presenter
        {
            get;
            internal set;
        }

        public double TopOffset
        {
            get;
            internal set;
        }

        private void RenderCell(DrawingContext dc, double top, double bottom, string text)
        {
            var formattedText = new FormattedText(text, System.Threading.Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft Sans Serif"),
                Presenter.ConvertToDeviceX(8, CellReport.Core.GraphicsUnit.Point),
                _textBrush, 
                new NumberSubstitution(),
                TextFormattingMode.Ideal);
            formattedText.MaxTextWidth = this.ActualWidth;
            formattedText.MaxTextHeight = (bottom - top) > 0 ? (bottom - top) : 0.01;
            formattedText.TextAlignment = TextAlignment.Center;
            var position = new Point(0, top + (formattedText.MaxTextHeight - formattedText.Height) / 2);
            dc.DrawText(formattedText, position);
            var pen = new Pen(_lineBrush, 1);
            dc.DrawLine(pen, new Point(0, bottom), new Point(this.ActualWidth, bottom));
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(_backBrush, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            var pen = new Pen(_lineBrush, 1);
            drawingContext.DrawLine(pen, new Point(0, 0), new Point(0, this.ActualHeight));
            drawingContext.DrawLine(pen, new Point(this.ActualWidth, 0), 
                new Point(this.ActualWidth, this.ActualHeight));
            if (Presenter != null)
            {
                foreach (var row in GetRowIndexes())
                {
                    var top = Presenter.GetTopOffset(row) + TopOffset;
                    var bottom = top + Presenter.GetRowHeight(row);

                    if (CellReport.Core.CellManager.IsDesignMode)
                        RenderCell(drawingContext, top, bottom, row.ToString());
                    else
                        RenderCell(drawingContext, top, bottom, (row + 1).ToString());
                }
            }
        }

        private IEnumerable<int> GetRowIndexes()
        {
            if (Presenter != null)
            {
                for (int row = 0; row < Presenter.FrozenRowsCount; row++)
                {
                    var top = Presenter.GetTopOffset(row) + TopOffset;
                    var bottom = top + Presenter.GetRowHeight(row);
                    if (bottom > this.ActualHeight)
                        break;

                    yield return row;
                }
                for (int row = Presenter.TopNoFrozenRow; row < Presenter.Manager.RowsCount; row++)
                {
                    var top = Presenter.GetTopOffset(row) + TopOffset;
                    var bottom = top + Presenter.GetRowHeight(row);
                    if (bottom > this.ActualHeight)
                        break;

                    yield return row;
                }
            }
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (Presenter != null)
            {
                var y = e.GetPosition(this).Y;
                if (_resizeRow < 0 && _startSelectionRow < 0)
                {
                    this.Cursor = Cursors.Arrow;
                    foreach (var row in GetRowIndexes())
                    {
                        var top = Presenter.GetTopOffset(row) + TopOffset;
                        var bottom = top + Presenter.GetRowHeight(row);

                        if (y <= bottom + 3 && y >= bottom - 3)
                            this.Cursor = Cursors.SizeNS;
                    }
                }
                else if (_resizeRow >= 0 && _lastY != y)
                {
                    var oldHeight = Presenter.GetRowHeight(_resizeRow);
                    var height = oldHeight + (y - _lastY);
                    Presenter.SetRowHeight(_resizeRow, Convert.ToSingle(height));
                }
                else if (_startSelectionRow >= 0 && _lastY != y)
                {
                    var findRow = -1;
                    foreach (var row in GetRowIndexes())
                    {
                        var top = Presenter.GetTopOffset(row);
                        if (top <= y && y <= top + Presenter.GetRowHeight(row))
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
                _lastY = y;
            }
        }

        private int _resizeRow = -1;
        private int _startSelectionRow = -1;
        private double _lastY = 0;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (this.Cursor == Cursors.SizeNS)
            {
                _resizeRow = -1;
                var y = e.GetPosition(this).Y;
                foreach (var row in GetRowIndexes())
                {
                    var top = Presenter.GetTopOffset(row);
                    var bottom = top + Presenter.GetRowHeight(row);

                    if (y <= bottom + 3 && y >= bottom - 3)
                    {
                        _resizeRow = row;
                        break;
                    }
                }
                if (_resizeRow >= 0)
                    Mouse.Capture(this);
            }
            else
            {
                _startSelectionRow = -1;
                var y = e.GetPosition(this).Y;
                foreach (var row in GetRowIndexes())
                {
                    var top = Presenter.GetTopOffset(row);
                    if (top <= y && y <= top + Presenter.GetRowHeight(row))
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
                    Mouse.Capture(this);
                }
            }
        }

        protected override void  OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            _resizeRow = -1;
            _startSelectionRow = -1;
            Mouse.Capture(null);
        }
    }
}
