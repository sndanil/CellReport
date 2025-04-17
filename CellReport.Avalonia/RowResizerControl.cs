using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CellReport.Presenters;
using System;
using System.Collections.Generic;

namespace CellReport.Avalonia
{
    public class RowResizerControl : Control
    {
        private static SolidColorBrush _lineBrush = new SolidColorBrush(Color.FromRgb(0xB1, 0xB5, 0xBA));
        private static SolidColorBrush _backBrush = new SolidColorBrush(Color.FromRgb(0xDF, 0xE3, 0xE8));
        private static SolidColorBrush _textBrush = new SolidColorBrush(Color.FromRgb(0x36, 0x36, 0x36));

        private bool _isPreresize = false;
        private int _resizeColumn = -1;
        private int _startSelectionColumn = -1;
        private double _lastX = 0;

        public BaseDisplayPresenter Presenter
        {
            get;
            set;
        }

        private void RenderCell(DrawingContext dc, float left, float right, string text)
        {
            var formattedText = new FormattedText(text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft Sans Serif"),
                10,
                _textBrush
            );

            formattedText.MaxTextHeight = Height;
            formattedText.MaxTextWidth = (right - left) > 0 ? (right - left) : 0.01;
            formattedText.TextAlignment = TextAlignment.Center;
            var position = new global::Avalonia.Point(left, (this.Bounds.Height - formattedText.LineHeight) / 2);
            dc.DrawText(formattedText, position);
            var pen = new Pen(_lineBrush, 1);
            dc.DrawLine(pen, new global::Avalonia.Point(right, 0), new global::Avalonia.Point(right, this.Bounds.Height));
        }

        public override void Render(DrawingContext drawingContext)
        {
            drawingContext.FillRectangle(_backBrush, this.Bounds);
            var pen = new Pen(_lineBrush, 1);
            drawingContext.DrawLine(pen, this.Bounds.TopLeft, this.Bounds.TopRight);
            drawingContext.DrawLine(pen, this.Bounds.BottomLeft, this.Bounds.BottomRight);
            if (Presenter != null)
            {
                foreach (var column in GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(column);
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
                    if (right > this.Bounds.Width)
                        break;

                    yield return column;
                }
                for (int column = Presenter.LeftNoFrozenColumn; column < Presenter.Manager.ColumnsCount; column++)
                {
                    var left = Presenter.GetLeftOffset(column);
                    var right = left + Presenter.GetColumnWidth(column);
                    if (right > this.Bounds.Width)
                        break;

                    yield return column;
                }
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (Presenter != null)
            {
                var x = e.GetPosition(this).X;
                if (_resizeColumn < 0 && _startSelectionColumn < 0)
                {
                    Cursor = global::Avalonia.Input.Cursor.Default;
                    _isPreresize = false;
                    foreach (var column in GetColumnIndexes())
                    {
                        var left = Presenter.GetLeftOffset(column);
                        var right = left + Presenter.GetColumnWidth(column);

                        if (x <= right + 3 && x >= right - 3)
                        {
                            this.Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.SizeWestEast);
                            _isPreresize = true;
                        }
                    }
                }
                else if (_resizeColumn >= 0 && _lastX != x)
                {
                    var oldWidth = Presenter.GetColumnWidth(_resizeColumn);
                    var width = oldWidth + (x - _lastX);
                    Presenter.SetColumnWidth(_resizeColumn, Convert.ToSingle(width));
                    Presenter.Update();
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

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            var x = e.GetPosition(this).X;
            if (_isPreresize)
            {
                _resizeColumn = -1;
                foreach (var column in GetColumnIndexes())
                {
                    var left = Presenter.GetLeftOffset(column);
                    var right = left + Presenter.GetColumnWidth(column);

                    if ((x <= right + 3) && (x >= right - 3) && (left < right)
                        && Core.MarkerResolver.Resolve(Presenter.Manager.GetColumnMarkerForShow(column))
                        && (string.IsNullOrEmpty(Presenter.Manager.GetColumnMarkerForHide(column)) || !Core.MarkerResolver.Resolve(Presenter.Manager.GetColumnMarkerForHide(column))))
                        _resizeColumn = column;
                }
                //if (_resizeColumn >= 0)
                //	 Mouse.Capture(this);
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
                    //Mouse.Capture(this);
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            _isPreresize = false;
            _resizeColumn = -1;
            _startSelectionColumn = -1;
            //Mouse.Capture(null);
        }
    }
}
