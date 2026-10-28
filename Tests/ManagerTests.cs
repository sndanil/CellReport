using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class ManagerTests
	{
		[TestMethod]
		public void RowsAndColumns_CanBeExpanded()
		{
			var manager = new CellManager(5, 5);
			manager.RowsCount = 30;
			manager.ColumnsCount = 20;

			Assert.AreEqual(30, manager.RowsCount);
			Assert.AreEqual(20, manager.ColumnsCount);
			Assert.IsNotNull(manager.CreateCell(29, 19));
		}

		[TestMethod]
		public void CreateCell_SetsLocation()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(2, 3);

			Assert.AreEqual(2, cell.Location.Row);
			Assert.AreEqual(3, cell.Location.Column);
		}

		[TestMethod]
		public void DesignMode_IsScopedToManagerInstance()
		{
			var designer = new CellManager { IsDesignMode = true };
			var runtime = new CellManager();

			Assert.IsTrue(designer.IsDesignMode);
			Assert.IsFalse(runtime.IsDesignMode);
		}

		[TestMethod]
		public void CellSpan_CoversMergedArea()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(2, 3);
			cell.Span = new CellSpan { RowSpan = 2, ColSpan = 3 };

			Assert.AreEqual(2, cell.Span.RowSpan);
			Assert.AreEqual(3, cell.Span.ColSpan);
			Assert.AreSame(cell, manager.GetCell(2, 3));

			cell = manager.CreateCell(0, 0);
			cell.Span = new CellSpan { RowSpan = 1, ColSpan = 1 };

			Assert.AreSame(cell, manager.GetCell(0, 0));
			Assert.AreSame(cell, manager.GetCell(1, 1));
			Assert.AreSame(cell, manager.GetCell(0, 1));
			Assert.AreSame(cell, manager.GetCell(1, 0));
		}

		[TestMethod]
		public void RowHeightAndColumnWidth_AffectCellSize()
		{
			var defaultColumn = new Column { Width = 50 };
			var manager = new CellManager(10, 10, 50, defaultColumn);
			var cell = manager.CreateCell(2, 3);
			manager.SetRowHeight(2, 100);
			manager.SetColumnWidth(3, 120);

			Assert.AreEqual(100, cell.Size.Height);
			Assert.AreEqual(120, cell.Size.Width);
			Assert.AreEqual(100, manager.GetRowHeight(2));
			Assert.AreEqual(120, manager.GetColumnWidth(3));
			Assert.AreEqual(50, manager.GetRowHeight(1));
			Assert.AreEqual(50, manager.GetColumnWidth(1));

			cell.Span = new CellSpan { ColSpan = 1, RowSpan = 1 };

			Assert.AreEqual(150, cell.Size.Height);
			Assert.AreEqual(170, cell.Size.Width);
		}

		[TestMethod]
		public void IdenticalStyles_AreInterned()
		{
			var manager = new CellManager();
			var cell = manager.CreateCell(2, 3);
			cell.Style = new CellStyle("#FFFFFF");

			var cell2 = manager.CreateCell(3, 3);
			cell2.Style = new CellStyle("#FFFFFF");

			Assert.AreSame(cell.Style, cell2.Style);
		}

		[TestMethod]
		public void RowsCount_CanGrowWithoutChangingColumns()
		{
			var manager = new CellManager(5, 5);
			manager.RowsCount = 30;

			Assert.AreEqual(30, manager.RowsCount);
			Assert.AreEqual(5, manager.ColumnsCount);
			Assert.IsNotNull(manager.CreateCell(29, 1));
		}

		[TestMethod]
		public void ColumnsCount_CanGrowWithoutChangingRows()
		{
			var manager = new CellManager(5, 5);
			manager.ColumnsCount = 30;

			Assert.AreEqual(5, manager.RowsCount);
			Assert.AreEqual(30, manager.ColumnsCount);
			Assert.IsNotNull(manager.CreateCell(1, 29));
		}

		[TestMethod]
		public void ActualDimensions_ReflectUsedCells()
		{
			var manager = new CellManager(5, 5);
			manager.ColumnsCount = 30;

			Assert.AreEqual(0, manager.ActualColumnsCount);
			Assert.AreEqual(0, manager.ActualRowsCount);

			manager.CreateCell(1, 29);

			Assert.AreEqual(30, manager.ActualColumnsCount);
			Assert.AreEqual(2, manager.ActualRowsCount);
		}
	}
}
