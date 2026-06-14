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
	public class ColumnResizerControl : BaseResizerControl
	{
		private int _resizeColumn = -1;
		private int _startSelectionColumn = -1;
		private double _lastX;

		public override void Render(DrawingContext drawingContext)
		{
			base.Render(drawingContext);

			if (Presenter == null)
				return;

			foreach (var column in GetColumnIndexes())
			{
				var left = Presenter.GetLeftOffset(column);
				var right = left + Presenter.GetColumnWidth(column);
				if (IsCurrentColumn(column))
					DrawCurrentCellHighlight(drawingContext, new AvaloniaRect(left, 0, right - left, Bounds.Height));
			}

			foreach (var column in GetColumnIndexes())
			{
				var left = Presenter.GetLeftOffset(column);
				var right = left + Presenter.GetColumnWidth(column);
				var columnName = CellManager.IsDesignMode ? column.ToString() : (column + 1).ToString();
				RenderText(drawingContext, columnName, new AvaloniaRect(left, 0, right - left, Bounds.Height));
				drawingContext.DrawLine(CreatePen(), new AvaloniaPoint(right, 0), new AvaloniaPoint(right, Bounds.Height));
			}

			drawingContext.DrawLine(CreatePen(), new AvaloniaPoint(0, 0), new AvaloniaPoint(Bounds.Width - 1, 0));
			drawingContext.DrawLine(CreatePen(), new AvaloniaPoint(0, Bounds.Height - 1), new AvaloniaPoint(Bounds.Width - 1, Bounds.Height - 1));
		}

		private IEnumerable<int> GetColumnIndexes()
		{
			if (Presenter == null)
				yield break;

			foreach (var columnIndex in Presenter.GetColumnIndexes())
			{
				var left = Presenter.GetLeftOffset(columnIndex);
				if (left > Bounds.Width)
					break;

				yield return columnIndex;
			}
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);

			if (Presenter == null)
				return;

			var x = e.GetPosition(this).X;
			if (_resizeColumn < 0 && _startSelectionColumn < 0)
			{
				Cursor = Cursor.Default;
				_isPreresize = false;
				foreach (var column in GetColumnIndexes())
				{
					var left = Presenter.GetLeftOffset(column);
					var right = left + Presenter.GetColumnWidth(column);
					if (x <= right + 3 && x >= right - 3)
					{
						Cursor = new Cursor(StandardCursorType.SizeWestEast);
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
					Select(
						new CellLocation { Row = 0, Column = _startSelectionColumn },
						new CellLocation { Row = Presenter.Manager.RowsCount - 1, Column = findCol });
				}
			}

			_lastX = x;
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);

			if (Presenter == null
				|| e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
				return;

			var x = e.GetPosition(this).X;
			if (_isPreresize)
			{
				_resizeColumn = -1;
				foreach (var column in GetColumnIndexes())
				{
					var left = Presenter.GetLeftOffset(column);
					var right = left + Presenter.GetColumnWidth(column);
					if (x <= right + 3 && x >= right - 3 && left < right
						&& MarkerResolver.Resolve(Presenter.Manager.GetColumnMarkerForShow(column))
						&& (string.IsNullOrEmpty(Presenter.Manager.GetColumnMarkerForHide(column))
							|| !MarkerResolver.Resolve(Presenter.Manager.GetColumnMarkerForHide(column))))
						_resizeColumn = column;
				}

				if (_resizeColumn >= 0)
				{
					e.Pointer.Capture(this);
					Presenter.State = PresenterState.ColumnResize;
					_lastX = x;
				}
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
					Select(
						new CellLocation { Row = 0, Column = _startSelectionColumn },
						new CellLocation { Row = Presenter.Manager.RowsCount - 1, Column = _startSelectionColumn });
					e.Pointer.Capture(this);
					_lastX = x;
				}
			}
		}

		protected override void OnPointerReleased(PointerReleasedEventArgs e)
		{
			base.OnPointerReleased(e);

			if (_resizeColumn >= 0 && Presenter != null)
				Presenter.State = PresenterState.Normal;

			_isPreresize = false;
			_resizeColumn = -1;
			_startSelectionColumn = -1;
			e.Pointer.Capture(null);
		}
	}
}
