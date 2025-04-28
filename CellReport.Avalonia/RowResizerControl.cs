using Avalonia.Input;
using Avalonia.Media;
using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaPoint = global::Avalonia.Point;
using AvaloniaRect = global::Avalonia.Rect;

namespace CellReport.Avalonia
{
    public class RowResizerControl : BaseResizerControl
    {
        private int _resizeRow = -1;
        private int _startSelectionRow = -1;
        private double _lastY = 0;

        public override void Render(DrawingContext drawingContext)
        {
            base.Render(drawingContext);
            drawingContext.DrawLine(CreatePen(), new AvaloniaPoint(0, 0), new AvaloniaPoint(this.Bounds.Width, 0));
            if (Presenter != null)
            {
                IterateByRows((row, top, bottom) =>
                {
                    var rowName = CellReport.Core.CellManager.IsDesignMode ? row.ToString() : (row + 1).ToString();
                    RenderText(drawingContext, rowName, new AvaloniaRect(0, top, Bounds.Width, bottom - top));
                    drawingContext.DrawLine(CreatePen(), new AvaloniaPoint(0, bottom), new AvaloniaPoint(this.Bounds.Width, bottom));
                });
            }
        }

        private void IterateByRows(Action<int, float, float> action)
        {
            foreach (var row in GetRowIndexes())
            {
                var top = Presenter.GetTopOffset(row);
                var bottom = top + Presenter.GetRowHeight(row);

                action?.Invoke(row, top, bottom);
            }

        }

        private IEnumerable<int> GetRowIndexes()
        {
            if (Presenter != null)
            {
                var allRows = Enumerable.Range(0, Presenter.FrozenRowsCount)
                    .Union(Enumerable.Range(Presenter.TopNoFrozenRow, Presenter.Manager.RowsCount - Presenter.TopNoFrozenRow))
                    .Distinct();
                foreach (var rowIndex in allRows)
                {
                    var top = Presenter.GetTopOffset(rowIndex);
                    var bottom = top + Presenter.GetRowHeight(rowIndex);
                    if (bottom > this.Bounds.Height)
                        break;

                    yield return rowIndex;
                }
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (Presenter != null)
            {
                var y = e.GetPosition(this).Y;
                if (_resizeRow < 0 && _startSelectionRow < 0)
                {
                    Cursor = global::Avalonia.Input.Cursor.Default;
                    _isPreresize = false;
                    IterateByRows((row, top, bottom) =>
                    {
                        if (y <= bottom + 3 && y >= bottom - 3)
                        {
                            this.Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.SizeNorthSouth);
                            _isPreresize = true;
                        }
                    });
                }
                else if (_resizeRow >= 0 && _lastY != y)
                {
                    var oldHeight = Presenter.GetRowHeight(_resizeRow);
                    var height = oldHeight + (y - _lastY);
                    Presenter.SetRowHeight(_resizeRow, Convert.ToSingle(height));
                    Presenter.Update();
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
                        Select(new CellLocation { Row = _startSelectionRow, Column = 0 }, 
                            new CellLocation() { Row = findRow, Column = Presenter.Manager.ColumnsCount - 1 });
                    }
                }
                _lastY = y;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            var y = e.GetPosition(this).Y;
            if (_isPreresize)
            {
                _resizeRow = -1;
                IterateByRows((row, top, bottom) =>
                {
                    if ((y <= bottom + 3) && (y >= bottom - 3) && (top < bottom))
                        _resizeRow = row;
                });
            }
            else
            {
                _startSelectionRow = -1;
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
                    Select(new CellLocation() { Row = _startSelectionRow, Column = 0 },
                        new CellLocation() { Row = _startSelectionRow, Column = Presenter.Manager.ColumnsCount - 1 });
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            _isPreresize = false;
            _resizeRow = -1;
            _startSelectionRow = -1;
        }
    }
}
