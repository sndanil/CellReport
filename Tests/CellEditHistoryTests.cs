using CellReport.Core;
using CellReport.Core.Contents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class CellEditHistoryTests
	{
		private static CellManager CreateManager()
		{
			return new CellManager(2, 2);
		}

		private static Cell CreateTextCell(CellManager manager, int row, int column, string text)
		{
			var cell = manager.CreateCell(row, column);
			cell.Editable = true;
			cell.Content = new TextContent(manager) { Text = text };
			return cell;
		}

		[TestMethod]
		public void Undo_RestoresPreviousTextAndRedoRestoresNew()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "old");

			var before = CellSnapshot.Capture(manager, cell.Location);
			((TextContent)cell.Content).Text = "new";
			var after = CellSnapshot.Capture(manager, cell.Location);

			var history = new CellEditHistory();
			history.Push(before, after);

			Assert.IsTrue(history.CanUndo);
			Assert.IsTrue(history.CanRedo == false);

			history.Undo(manager);
			Assert.AreEqual("old", ((TextContent)manager.GetCell(0, 0).Content).Text);
			Assert.IsTrue(history.CanRedo);

			history.Redo(manager);
			Assert.AreEqual("new", ((TextContent)manager.GetCell(0, 0).Content).Text);
		}

		[TestMethod]
		public void Undo_CreationOfEmptyCell_RemovesCell()
		{
			var manager = CreateManager();
			var location = new CellLocation { Row = 1, Column = 1 };
			var before = CellSnapshot.Capture(manager, location);

			var cell = manager.CreateCell(location);
			cell.Editable = true;
			cell.Content = new TextContent(manager) { Text = "value" };
			var after = CellSnapshot.Capture(manager, location);

			var history = new CellEditHistory();
			history.Push(before, after);
			history.Undo(manager);

			Assert.IsNull(manager.GetCell(location));

			history.Redo(manager);
			var restored = manager.GetCell(location);
			Assert.IsNotNull(restored);
			Assert.AreEqual("value", ((TextContent)restored.Content).Text);
			Assert.IsTrue(restored.Editable);
		}

		[TestMethod]
		public void Undo_RestoresEditFlagsAndPlaceholder()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "text");
			cell.EditParam = "ФИО";
			cell.EditPlaceholder = "Фамилия";

			var before = CellSnapshot.Capture(manager, cell.Location);
			cell.EditParam = null;
			cell.EditPlaceholder = null;
			cell.Editable = false;
			var after = CellSnapshot.Capture(manager, cell.Location);

			var history = new CellEditHistory();
			history.Push(before, after);
			history.Undo(manager);

			var restored = manager.GetCell(0, 0);
			Assert.IsTrue(restored.Editable);
			Assert.AreEqual("ФИО", restored.EditParam);
			Assert.AreEqual("Фамилия", restored.EditPlaceholder);
		}

		[TestMethod]
		public void History_LimitDropsOldestEntries()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "0");

			var history = new CellEditHistory { Limit = 2 };
			for (var i = 1; i <= 3; i++)
			{
				var before = CellSnapshot.Capture(manager, cell.Location);
				((TextContent)cell.Content).Text = i.ToString();
				var after = CellSnapshot.Capture(manager, cell.Location);
				history.Push(before, after);
			}

			history.Undo(manager);
			Assert.AreEqual("2", ((TextContent)cell.Content).Text);
			history.Undo(manager);
			Assert.AreEqual("1", ((TextContent)cell.Content).Text);
			Assert.IsFalse(history.CanUndo);
		}

		[TestMethod]
		public void Push_ClearsRedo()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "a");

			var history = new CellEditHistory();
			var before = CellSnapshot.Capture(manager, cell.Location);
			((TextContent)cell.Content).Text = "b";
			var after = CellSnapshot.Capture(manager, cell.Location);
			history.Push(before, after);

			history.Undo(manager);
			Assert.IsTrue(history.CanRedo);

			var before2 = CellSnapshot.Capture(manager, cell.Location);
			((TextContent)cell.Content).Text = "c";
			var after2 = CellSnapshot.Capture(manager, cell.Location);
			history.Push(before2, after2);

			Assert.IsFalse(history.CanRedo);
		}
	}
}
