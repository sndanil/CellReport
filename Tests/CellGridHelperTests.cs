using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class CellGridHelperTests
	{
		[TestMethod]
		public void Covers_ReturnsTrueInsideSpan()
		{
			var manager = new CellManager(5, 5);
			var cell = manager.CreateCell(1, 2);
			cell.Span = new CellSpan { RowSpan = 2, ColSpan = 1 };

			Assert.IsTrue(CellGridHelper.Covers(cell, 2, 2));
			Assert.IsTrue(CellGridHelper.Covers(cell, 3, 3));
			Assert.IsFalse(CellGridHelper.Covers(cell, 0, 2));
		}

		[TestMethod]
		public void RecalculateActualDimensions_UsesSpanExtent()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(2, 3);
			cell.Span = new CellSpan { RowSpan = 1, ColSpan = 2 };

			int rows;
			int columns;
			CellGridHelper.RecalculateActualDimensions(new System.Collections.Generic.List<Cell> { cell }, out rows, out columns);

			Assert.AreEqual(4, rows);
			Assert.AreEqual(6, columns);
		}
	}
}
