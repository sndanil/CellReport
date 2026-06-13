using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Presenters;
using System.Collections.Generic;

namespace CellReport.Avalonia.Services
{
	public sealed class FindResultItem
	{
		public string Text { get; init; } = string.Empty;
		public int Row { get; init; }
		public int Column { get; init; }
	}

	public enum SearchLookOver
	{
		ByRows,
		ByColumns
	}

	public enum SearchDirection
	{
		Forward,
		Backward
	}

	public static class ReportSearchService
	{
		public static bool TryFindNext(
			BaseDisplayPresenter presenter,
			string searchText,
			bool matchCase,
			SearchLookOver lookOver,
			SearchDirection direction,
			ref Cell? lastCell,
			out FindResultItem? result)
		{
			result = null;
			if (presenter == null || string.IsNullOrEmpty(searchText))
				return false;

			var manager = presenter.Manager;
			var current = presenter.CurrentCell;

			if (lookOver == SearchLookOver.ByRows)
			{
				if (direction == SearchDirection.Forward)
				{
					for (int row = current.Row; row < manager.RowsCount; row++)
					{
						var startCol = row == current.Row ? current.Column : 0;
						for (int col = startCol; col < manager.ColumnsCount; col++)
						{
							if (TryCheckCell(manager.GetCell(row, col), searchText, matchCase, ref lastCell, out var text))
							{
								presenter.CurrentCell = new CellLocation { Row = row, Column = col };
								result = new FindResultItem { Text = text!, Row = row, Column = col };
								return true;
							}
						}
					}
				}
				else
				{
					for (int row = current.Row; row >= 0; row--)
					{
						var startCol = row == current.Row ? current.Column : manager.ColumnsCount - 1;
						for (int col = startCol; col >= 0; col--)
						{
							if (TryCheckCell(manager.GetCell(row, col), searchText, matchCase, ref lastCell, out var text))
							{
								presenter.CurrentCell = new CellLocation { Row = row, Column = col };
								result = new FindResultItem { Text = text!, Row = row, Column = col };
								return true;
							}
						}
					}
				}
			}
			else
			{
				if (direction == SearchDirection.Forward)
				{
					for (int col = current.Column; col < manager.ColumnsCount; col++)
					{
						var startRow = col == current.Column ? current.Row : 0;
						for (int row = startRow; row < manager.RowsCount; row++)
						{
							if (TryCheckCell(manager.GetCell(row, col), searchText, matchCase, ref lastCell, out var text))
							{
								presenter.CurrentCell = new CellLocation { Row = row, Column = col };
								result = new FindResultItem { Text = text!, Row = row, Column = col };
								return true;
							}
						}
					}
				}
				else
				{
					for (int col = current.Column; col >= 0; col--)
					{
						var startRow = col == current.Column ? current.Row : manager.RowsCount - 1;
						for (int row = startRow; row >= 0; row--)
						{
							if (TryCheckCell(manager.GetCell(row, col), searchText, matchCase, ref lastCell, out var text))
							{
								presenter.CurrentCell = new CellLocation { Row = row, Column = col };
								result = new FindResultItem { Text = text!, Row = row, Column = col };
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		public static List<FindResultItem> FindAll(
			BaseDisplayPresenter presenter,
			string searchText,
			bool matchCase,
			SearchLookOver lookOver,
			SearchDirection direction)
		{
			var results = new List<FindResultItem>();
			if (presenter == null || string.IsNullOrEmpty(searchText))
				return results;

			Cell? lastCell = null;
			var manager = presenter.Manager;

			if (lookOver == SearchLookOver.ByRows)
			{
				if (direction == SearchDirection.Forward)
				{
					for (int row = 0; row < manager.RowsCount; row++)
						for (int col = 0; col < manager.ColumnsCount; col++)
							AddIfFound(manager.GetCell(row, col), searchText, matchCase, ref lastCell, row, col, results);
				}
				else
				{
					for (int row = manager.RowsCount - 1; row >= 0; row--)
						for (int col = manager.ColumnsCount - 1; col >= 0; col--)
							AddIfFound(manager.GetCell(row, col), searchText, matchCase, ref lastCell, row, col, results);
				}
			}
			else
			{
				if (direction == SearchDirection.Forward)
				{
					for (int col = 0; col < manager.ColumnsCount; col++)
						for (int row = 0; row < manager.RowsCount; row++)
							AddIfFound(manager.GetCell(row, col), searchText, matchCase, ref lastCell, row, col, results);
				}
				else
				{
					for (int col = manager.ColumnsCount - 1; col >= 0; col--)
						for (int row = manager.RowsCount - 1; row >= 0; row--)
							AddIfFound(manager.GetCell(row, col), searchText, matchCase, ref lastCell, row, col, results);
				}
			}

			return results;
		}

		private static void AddIfFound(Cell? cell, string searchText, bool matchCase, ref Cell? lastCell, int row, int col, List<FindResultItem> results)
		{
			if (TryCheckCell(cell, searchText, matchCase, ref lastCell, out var text))
				results.Add(new FindResultItem { Text = text!, Row = row, Column = col });
		}

		private static bool TryCheckCell(Cell? cell, string searchText, bool matchCase, ref Cell? lastCell, out string? text)
		{
			text = null;
			if (cell == null || lastCell == cell)
				return false;

			if (cell.Content is TextBasedContent textContent)
			{
				if (textContent.FindText(searchText, matchCase))
				{
					lastCell = cell;
					text = textContent.GetTextForRender();
					return true;
				}
			}
			else if (cell.Content is CollectionContent collection)
			{
				foreach (var item in collection.GetContents())
				{
					if (item is TextBasedContent nested && nested.FindText(searchText, matchCase))
					{
						lastCell = cell;
						text = nested.GetTextForRender();
						return true;
					}
				}
			}

			return false;
		}
	}
}
