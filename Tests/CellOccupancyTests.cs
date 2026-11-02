using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class CellOccupancyTests
	{
		[TestMethod]
		public void EmptySlot_IsNotOccupied()
		{
			var manager = new CellManager(2, 2);

			Cell cell;
			Assert.IsFalse(manager.TryGetOccupiedCell(0, 0, out cell));
			Assert.IsFalse(manager.IsSlotOccupied(0, 0));
		}

		[TestMethod]
		public void OriginCell_OccupiesSlot()
		{
			var manager = new CellManager(2, 2);
			var created = manager.CreateCell(1, 1);

			Cell cell;
			Assert.IsTrue(manager.TryGetOccupiedCell(1, 1, out cell));
			Assert.AreSame(created, cell);
		}

		[TestMethod]
		public void MergedCell_OccupiesCoveredSlot()
		{
			var manager = new CellManager(2, 3);
			var origin = manager.CreateCell(0, 0);
			origin.Span = new CellSpan { ColSpan = 1 };

			Cell covered;
			Assert.IsTrue(manager.TryGetOccupiedCell(0, 1, out covered));
			Assert.AreSame(origin, covered);
			Assert.IsTrue(manager.IsSlotOccupied(0, 1));
		}

		[TestMethod]
		public void ResetSpan_ReleasesCoveredSlot()
		{
			var manager = new CellManager(2, 3);
			var origin = manager.CreateCell(0, 0);
			origin.Span = new CellSpan { ColSpan = 1 };
			origin.Span = new CellSpan();

			Assert.IsFalse(manager.IsSlotOccupied(0, 1));
		}

		[TestMethod]
		public void DeleteCell_ReleasesSlot()
		{
			var manager = new CellManager(2, 2);
			manager.CreateCell(0, 0);
			manager.DeleteCell(0, 0);

			Assert.IsFalse(manager.IsSlotOccupied(0, 0));
		}

		[TestMethod]
		public void OutOfRangeSlot_IsNotOccupied()
		{
			var manager = new CellManager(1, 1);

			Assert.IsFalse(manager.IsSlotOccupied(-1, 0));
			Assert.IsFalse(manager.IsSlotOccupied(0, 5));
		}
	}
}
