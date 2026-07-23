using System;
using System.ComponentModel;
using CellReport;
using CellReport.Core;
using CellReport.Core.Expressions;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class CellManagerOptimizationTests
	{
		[TestMethod]
		public void BeginUpdate_BatchesCellsChangeNotifications()
		{
			var manager = new CellManager(10, 10);
			var cellsNotifications = 0;
			manager.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Cells")
					cellsNotifications++;
			};

			manager.BeginUpdate();
			for (int i = 0; i < 10; i++)
				manager.CreateCell(i, 0);
			Assert.AreEqual(0, cellsNotifications);

			manager.EndUpdate();
			Assert.AreEqual(1, cellsNotifications);
		}

		[TestMethod]
		public void BeginUpdate_NestedScopes_FlushesOnceAtOuterEnd()
		{
			var manager = new CellManager(10, 10);
			var cellsNotifications = 0;
			manager.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Cells")
					cellsNotifications++;
			};

			manager.BeginUpdate();
			manager.BeginUpdate();
			manager.CreateCell(0, 0);
			manager.EndUpdate();
			Assert.AreEqual(0, cellsNotifications);

			manager.EndUpdate();
			Assert.AreEqual(1, cellsNotifications);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void EndUpdate_WithoutBeginUpdate_Throws()
		{
			new CellManager().EndUpdate();
		}

		[TestMethod]
		public void BeginUpdateScope_DisposesAndFlushesNotifications()
		{
			var manager = new CellManager(10, 10);
			var cellsNotifications = 0;
			manager.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Cells")
					cellsNotifications++;
			};

			using (manager.BeginUpdateScope())
			{
				manager.CreateCell(1, 1);
				manager.CreateCell(2, 2);
				Assert.AreEqual(0, cellsNotifications);
			}

			Assert.AreEqual(1, cellsNotifications);
		}

		[TestMethod]
		public void TryGetCell_ReturnsFalseWhenOutOfBounds()
		{
			var manager = new CellManager(5, 5);
			Cell cell;

			Assert.IsFalse(manager.TryGetCell(-1, 0, out cell));
			Assert.IsFalse(manager.TryGetCell(0, -1, out cell));
			Assert.IsFalse(manager.TryGetCell(5, 0, out cell));
			Assert.IsFalse(manager.TryGetCell(0, 5, out cell));
			Assert.IsNull(cell);
			Assert.AreEqual(5, manager.RowsCount);
			Assert.AreEqual(5, manager.ColumnsCount);
		}

		[TestMethod]
		public void TryGetCell_ReturnsCellWithoutExpandingGrid()
		{
			var manager = new CellManager(5, 5);
			var created = manager.CreateCell(2, 3);
			Cell cell;

			Assert.IsTrue(manager.TryGetCell(2, 3, out cell));
			Assert.AreSame(created, cell);
			Assert.IsFalse(manager.TryGetCell(4, 4, out cell));
			Assert.IsNull(cell);
		}

		[TestMethod]
		public void GetExpressionByID_ReturnsRegisteredExpression()
		{
			var manager = new CellManager();
			var expression = new SumExpression { ID = "Total", ParamName = "Amount" };
			manager.AddExpression(expression);

			Assert.AreSame(expression, manager.GetExpressionByID("Total"));
			Assert.IsNull(manager.GetExpressionByID("Missing"));
		}

		[TestMethod]
		public void ReplaceExpression_UpdatesLookupById()
		{
			var manager = new CellManager();
			var original = new SumExpression { ID = "Total", ParamName = "Amount" };
			var replacement = new SumExpression { ID = "Total", ParamName = "Qty" };
			manager.AddExpression(original);
			manager.ReplaceExpression(replacement);

			Assert.AreSame(replacement, manager.GetExpressionByID("Total"));
		}

		[TestMethod]
		public void AddPageBreakRow_AllowsDuplicateAdds()
		{
			var manager = new CellManager();
			manager.AddPageBreakRow(3);
			manager.AddPageBreakRow(3);

			Assert.IsTrue(manager.IsPageBreakRow(3));
		}

		[TestMethod]
		public void DeleteCell_RecalculatesActualRowsWhenLastRowRemoved()
		{
			var manager = new CellManager(10, 10);
			manager.CreateCell(0, 0);
			manager.CreateCell(2, 0);

			Assert.AreEqual(3, manager.ActualRowsCount);

			manager.DeleteCell(2, 0);

			Assert.AreEqual(1, manager.ActualRowsCount);
			Assert.AreEqual(1, manager.ActualColumnsCount);
		}

		[TestMethod]
		public void DeleteCell_RecalculatesActualColumnsWhenLastColumnRemoved()
		{
			var manager = new CellManager(10, 10);
			manager.CreateCell(0, 0);
			manager.CreateCell(0, 4);

			Assert.AreEqual(5, manager.ActualColumnsCount);

			manager.DeleteCell(0, 4);

			Assert.AreEqual(1, manager.ActualColumnsCount);
			Assert.AreEqual(1, manager.ActualRowsCount);
		}

		[TestMethod]
		public void ReportDocument_AddSection_BatchesCellNotifications()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.SimpleTextRow);
			var cellsNotifications = 0;
			report.Manager.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "Cells")
					cellsNotifications++;
			};

			for (int i = 0; i < 5; i++)
			{
				report.AddSection("row");
				report.SetParameter("label", "Item " + i);
				report.SetParameter("value", i.ToString());
			}

			Assert.AreEqual(5, cellsNotifications);
		}
	}
}
