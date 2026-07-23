using CellReport;
using CellReport.Core;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class SparseGridTests
	{
		[TestMethod]
		public void MergedCell_IsStoredOnlyAtOrigin()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(0, 0);
			cell.Span = new CellSpan { RowSpan = 1, ColSpan = 1 };

			Assert.AreSame(cell, manager.GetStoredCell(0, 0));
			Assert.IsNull(manager.GetStoredCell(1, 1));
			Assert.AreEqual(1, manager.OriginCellCount);
		}

		[TestMethod]
		public void GetCell_ReturnsMergedCellForNonOriginSlot()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(2, 3);
			cell.Span = new CellSpan { RowSpan = 2, ColSpan = 3 };

			Assert.AreSame(cell, manager.GetCell(2, 3));
			Assert.AreSame(cell, manager.GetCell(4, 6));
			Assert.AreSame(cell, manager.GetCell(3, 5));
		}

		[TestMethod]
		public void TryGetCell_ReturnsMergedCellForNonOriginSlot()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(0, 0);
			cell.Span = new CellSpan { RowSpan = 1, ColSpan = 1 };
			Cell resolved;

			Assert.IsTrue(manager.TryGetCell(1, 0, out resolved));
			Assert.AreSame(cell, resolved);
		}

		[TestMethod]
		public void DeleteCell_FromMergedArea_RemovesWholeCell()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(0, 0);
			cell.Span = new CellSpan { RowSpan = 1, ColSpan = 1 };

			manager.DeleteCell(1, 1);

			Assert.IsNull(manager.GetCell(0, 0));
			Assert.IsNull(manager.GetStoredCell(0, 0));
			Assert.AreEqual(0, manager.OriginCellCount);
		}

		[TestMethod]
		public void CreateCell_OnMergedArea_ReplacesExistingCell()
		{
			var manager = new CellManager(10, 10);
			var merged = manager.CreateCell(0, 0);
			merged.Span = new CellSpan { RowSpan = 1, ColSpan = 1 };

			var replacement = manager.CreateCell(1, 1);

			Assert.AreNotSame(merged, replacement);
			Assert.AreSame(replacement, manager.GetCell(1, 1));
			Assert.IsNull(manager.GetCell(0, 0));
			Assert.AreEqual(1, manager.OriginCellCount);
		}

		[TestMethod]
		public void ActualDimensions_IncludeMergedSpan()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(1, 1);
			cell.Span = new CellSpan { RowSpan = 2, ColSpan = 3 };

			Assert.AreEqual(4, manager.ActualRowsCount);
			Assert.AreEqual(5, manager.ActualColumnsCount);
		}

		[TestMethod]
		public void NormalizeSparseGrid_RemovesLegacyDuplicateReferences()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(0, 0);
			cell.Span = new CellSpan { RowSpan = 1, ColSpan = 1 };
			manager.SetStoredCellForTests(1, 1, cell);

			manager.NormalizeSparseGrid();

			Assert.IsNull(manager.GetStoredCell(1, 1));
			Assert.AreSame(cell, manager.GetCell(1, 1));
			Assert.AreEqual(1, manager.OriginCellCount);
		}

		[TestMethod]
		public void ReportDocument_AddSection_WorksWithSparseGrid()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.HeaderAndCellSections);
			report.AddSection("header");
			report.SetParameter("header", "Title");
			report.AddSection("cell");
			report.SetParameter("cell", "Value");

			Assert.AreEqual("Title", GetText(report.Manager, 0, 0));
			Assert.AreEqual("Value", GetText(report.Manager, 1, 0));
			Assert.IsTrue(report.Manager.OriginCellCount > 0);
		}

		[TestMethod]
		public void SetSpansForCell_ReducingSpan_ClearsOldCoverage()
		{
			var manager = new CellManager(10, 10);
			var cell = manager.CreateCell(0, 0);
			cell.Span = new CellSpan { RowSpan = 2, ColSpan = 2 };
			manager.SetStoredCellForTests(2, 2, cell);

			cell.Span = new CellSpan { RowSpan = 0, ColSpan = 0 };

			Assert.IsNull(manager.GetStoredCell(2, 2));
			Assert.IsNull(manager.GetCell(2, 2));
		}

		private static string GetText(CellManager manager, int row, int column)
		{
			var cell = manager.GetCell(row, column);
			Assert.IsNotNull(cell);
			return ((CellReport.Core.Contents.TextContent)cell.Content).GetText();
		}
	}
}
