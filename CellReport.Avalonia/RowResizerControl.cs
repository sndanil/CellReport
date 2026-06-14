using Avalonia.Input;
using Avalonia.Media;
using CellReport.Core;
using CellReport.Presenters;
using System;
using System.Collections.Generic;
using AvaloniaPoint = global::Avalonia.Point;
using AvaloniaRect = global::Avalonia.Rect;

namespace CellReport.Avalonia
{
	public class RowResizerControl : BaseResizerControl
	{
		private int _resizeRow = -1;
		private int _startSelectionRow = -1;
		private double _lastY;

		public double TopOffset { get; set; }

		public override void Render(DrawingContext drawingContext)
		{
			base.Render(drawingContext);

			if (Presenter == null)
				return;

			drawingContext.DrawLine(
				CreateHeaderSeparatorPen(),
				new AvaloniaPoint(0, TopOffset - 1),
				new AvaloniaPoint(Bounds.Width, TopOffset - 1));

			foreach (var row in GetRowIndexes())
			{
				var top = Presenter.GetTopOffset(row) + (float)TopOffset;
				var bottom = top + Presenter.GetRowHeight(row);
				if (IsCurrentRow(row))
					DrawCurrentCellHighlight(drawingContext, new AvaloniaRect(0, top, Bounds.Width, bottom - top));
			}

			foreach (var row in GetRowIndexes())
			{
				var top = Presenter.GetTopOffset(row) + (float)TopOffset;
				var bottom = top + Presenter.GetRowHeight(row);
				var rowName = CellManager.IsDesignMode ? row.ToString() : (row + 1).ToString();
				RenderText(drawingContext, rowName, new AvaloniaRect(0, top, Bounds.Width, bottom - top));
				drawingContext.DrawLine(CreatePen(), new AvaloniaPoint(0, bottom), new AvaloniaPoint(Bounds.Width, bottom));
			}

			drawingContext.DrawRectangle(CreatePen(), new AvaloniaRect(0, 0, Bounds.Width - 1, Bounds.Height - 1));
		}

		private IEnumerable<int> GetRowIndexes()
		{
			if (Presenter == null)
				yield break;

			foreach (var rowIndex in Presenter.GetRowIndexes())
			{
				var top = Presenter.GetTopOffset(rowIndex) + TopOffset;
				if (top > Bounds.Height)
					break;

				yield return rowIndex;
			}
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);

			if (Presenter == null)
				return;

			var y = e.GetPosition(this).Y;
			if (_resizeRow < 0 && _startSelectionRow < 0)
			{
				Cursor = Cursor.Default;
				_isPreresize = false;
				foreach (var row in GetRowIndexes())
				{
					var top = Presenter.GetTopOffset(row) + TopOffset;
					var bottom = top + Presenter.GetRowHeight(row);
					if (y <= bottom + 3 && y >= bottom - 3)
					{
						Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
						_isPreresize = true;
					}
				}
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
					var top = Presenter.GetTopOffset(row) + TopOffset;
					if (top <= y && y <= top + Presenter.GetRowHeight(row))
					{
						findRow = row;
						break;
					}
				}

				if (findRow >= 0)
				{
					Select(
						new CellLocation { Row = _startSelectionRow, Column = 0 },
						new CellLocation { Row = findRow, Column = Presenter.Manager.ColumnsCount - 1 });
				}
			}

			_lastY = y;
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);

			if (Presenter == null
				|| e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
				return;

			var y = e.GetPosition(this).Y;
			if (_isPreresize)
			{
				_resizeRow = -1;
				foreach (var row in GetRowIndexes())
				{
					var top = Presenter.GetTopOffset(row) + TopOffset;
					var bottom = top + Presenter.GetRowHeight(row);
					if (y <= bottom + 3 && y >= bottom - 3 && top < bottom)
						_resizeRow = row;
				}

				if (_resizeRow >= 0)
				{
					e.Pointer.Capture(this);
					Presenter.State = PresenterState.RowResize;
					_lastY = y;
				}
			}
			else
			{
				_startSelectionRow = -1;
				foreach (var row in GetRowIndexes())
				{
					var top = Presenter.GetTopOffset(row) + TopOffset;
					if (top <= y && y <= top + Presenter.GetRowHeight(row))
					{
						_startSelectionRow = row;
						break;
					}
				}

				if (_startSelectionRow >= 0)
				{
					Select(
						new CellLocation { Row = _startSelectionRow, Column = 0 },
						new CellLocation { Row = _startSelectionRow, Column = Presenter.Manager.ColumnsCount - 1 });
					e.Pointer.Capture(this);
					_lastY = y;
				}
			}
		}

		protected override void OnPointerReleased(PointerReleasedEventArgs e)
		{
			base.OnPointerReleased(e);

			if (_resizeRow >= 0 && Presenter != null)
				Presenter.State = PresenterState.Normal;

			_isPreresize = false;
			_resizeRow = -1;
			_startSelectionRow = -1;
			e.Pointer.Capture(null);
		}
	}
}
