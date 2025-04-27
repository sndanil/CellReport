using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaPoint = global::Avalonia.Point;
using AvaloniaRect = global::Avalonia.Rect;

namespace CellReport.Avalonia
{
    public class ColumnResizerControl : BaseResizerControl
    {
        private int _resizeColumn = -1;
        private int _startSelectionColumn = -1;
        private double _lastX = 0;

        public override void Render(DrawingContext drawingContext)
        {
            base.Render(drawingContext);
            drawingContext.DrawLine(CreatePen(), new AvaloniaPoint(0, 0), new AvaloniaPoint(0, this.Bounds.Height));
            if (Presenter != null)
            {
                IterateByColumns((column, left, right) =>
                {
                    var columnName = CellReport.Core.CellManager.IsDesignMode ? column.ToString() : (column + 1).ToString();
                    RenderText(drawingContext, columnName, new AvaloniaRect(left, 0, right - left, Bounds.Height));
                    drawingContext.DrawLine(CreatePen(), new AvaloniaPoint(right, 0), new AvaloniaPoint(right, this.Bounds.Height));
                });
            }
        }

        private void IterateByColumns(Action<int, float, float> action)
        {
            foreach (var column in GetColumnIndexes())
            {
                var left = Presenter.GetLeftOffset(column);
                var right = left + Presenter.GetColumnWidth(column);

                action?.Invoke(column, left, right);
            }

        }

        private IEnumerable<int> GetColumnIndexes()
        {
            if (Presenter != null)
            {
                var allColumns = Enumerable.Range(0, Presenter.FrozenColumnsCount)
                    .Union(Enumerable.Range(Presenter.LeftNoFrozenColumn, Presenter.Manager.ColumnsCount - Presenter.LeftNoFrozenColumn))
                    .Distinct();
                foreach (var columnIndex in allColumns)
                {
                    var left = Presenter.GetLeftOffset(columnIndex);
                    var right = left + Presenter.GetColumnWidth(columnIndex);
                    if (right > this.Bounds.Width)
                        break;

                    yield return columnIndex;
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
                    IterateByColumns((column, left, right) =>
                    {
                        if (x <= right + 3 && x >= right - 3)
                        {
                            this.Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.SizeWestEast);
                            _isPreresize = true;
                        }
                    });
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
                IterateByColumns((column, left, right) =>
                {
                    if ((x <= right + 3) && (x >= right - 3) && (left < right)
                        && Core.MarkerResolver.Resolve(Presenter.Manager.GetColumnMarkerForShow(column))
                        && (string.IsNullOrEmpty(Presenter.Manager.GetColumnMarkerForHide(column)) || !Core.MarkerResolver.Resolve(Presenter.Manager.GetColumnMarkerForHide(column))))
                        _resizeColumn = column;
                });
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
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            _isPreresize = false;
            _resizeColumn = -1;
            _startSelectionColumn = -1;
        }
    }
}
