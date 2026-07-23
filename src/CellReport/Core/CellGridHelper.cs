using System.Collections.Generic;

namespace CellReport.Core
{
	internal static class CellGridHelper
	{
		public static bool Covers(Cell cell, int row, int column)
		{
			if (cell == null)
				return false;

			var span = cell.SpanInternal;
			return row >= cell.Location.Row && row <= cell.Location.Row + span.RowSpan
				&& column >= cell.Location.Column && column <= cell.Location.Column + span.ColSpan;
		}

		public static bool IsOrigin(Cell cell, int row, int column)
		{
			return cell != null && cell.Location.Row == row && cell.Location.Column == column;
		}

		public static void ClearSpanSlotReferences(Cell[][] cells, CellLocation origin, CellSpan span, Cell cell)
		{
			for (int row = origin.Row; row <= origin.Row + span.RowSpan; row++)
			{
				for (int col = origin.Column; col <= origin.Column + span.ColSpan; col++)
				{
					if (row == origin.Row && col == origin.Column)
						continue;

					if (cells[row][col] == cell)
						cells[row][col] = null;
				}
			}
		}

		public static void RecalculateActualDimensions(IList<Cell> originCells, out int actualRowsCount, out int actualColumnsCount)
		{
			actualRowsCount = 0;
			actualColumnsCount = 0;

			for (int i = 0; i < originCells.Count; i++)
			{
				var cell = originCells[i];
				if (cell == null)
					continue;

				var span = cell.SpanInternal;
				actualColumnsCount = System.Math.Max(actualColumnsCount, cell.Location.Column + span.ColSpan + 1);
				actualRowsCount = System.Math.Max(actualRowsCount, cell.Location.Row + span.RowSpan + 1);
			}
		}
	}
}
