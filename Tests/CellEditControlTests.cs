#if NET10_0
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using CellReport.Avalonia;
using CellReport.Core;
using CellReport.Core.Contents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CellReportTests
{
	[TestClass]
	[DoNotParallelize]
	public class CellEditControlTests
	{
		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			if (Application.Current == null)
			{
				AppBuilder.Configure<TestApplication>()
					.UseHeadless(new AvaloniaHeadlessPlatformOptions())
					.SetupWithoutStarting();
			}
		}

		private static CellManager CreateManager(CellEditMode mode)
		{
			var manager = new CellManager(2, 2);
			manager.EditSettings.Mode = mode;
			return manager;
		}

		private static Cell CreateTextCell(CellManager manager, int row, int column, string text, bool editable)
		{
			var cell = manager.CreateCell(row, column);
			cell.Editable = editable;
			cell.Content = new TextContent(manager) { Text = text };
			return cell;
		}

		[TestMethod]
		public void BeginEdit_MarkedCell_CommitsAndRaisesEvent()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			var cell = CreateTextCell(manager, 0, 0, "old", editable: true);
			var control = new PresentControl();
			control.AttachManager(manager);

			var edited = 0;
			control.CellEdited += (_, _) => edited++;

			Assert.IsTrue(control.BeginCellEdit(cell.Location));
			Assert.IsTrue(control.IsCellEditing);
			Assert.AreEqual("old", control.CellEditText);

			control.CellEditText = "new";
			control.CommitCellEdit();

			Assert.IsFalse(control.IsCellEditing);
			Assert.AreEqual("new", ((TextContent)cell.Content).Text);
			Assert.AreEqual(1, edited);
			Assert.IsTrue(control.CanUndoCellEdit);

			Assert.IsTrue(control.UndoCellEdit());
			Assert.AreEqual("old", ((TextContent)cell.Content).Text);

			Assert.IsTrue(control.RedoCellEdit());
			Assert.AreEqual("new", ((TextContent)cell.Content).Text);
		}

		[TestMethod]
		public void BeginEdit_UnmarkedCell_ReturnsFalse()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			var cell = CreateTextCell(manager, 0, 0, "text", editable: false);
			var control = new PresentControl();
			control.AttachManager(manager);

			Assert.IsFalse(control.BeginCellEdit(cell.Location));
			Assert.IsFalse(control.IsCellEditing);
		}

		[TestMethod]
		public void BeginEdit_NoneMode_ReturnsFalse()
		{
			var manager = CreateManager(CellEditMode.None);
			var cell = CreateTextCell(manager, 0, 0, "text", editable: true);
			var control = new PresentControl();
			control.AttachManager(manager);

			Assert.IsFalse(control.BeginCellEdit(cell.Location));
		}

		[TestMethod]
		public void Cancel_DoesNotChangeValue()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			var cell = CreateTextCell(manager, 0, 0, "old", editable: true);
			var control = new PresentControl();
			control.AttachManager(manager);

			Assert.IsTrue(control.BeginCellEdit(cell.Location));
			control.CellEditText = "new";
			control.CancelCellEdit();

			Assert.IsFalse(control.IsCellEditing);
			Assert.AreEqual("old", ((TextContent)cell.Content).Text);
			Assert.IsFalse(control.CanUndoCellEdit);
		}

		[TestMethod]
		public void InvalidNumber_KeepsEditorOpenAndModelUnchanged()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			var cell = manager.CreateCell(0, 0);
			cell.Editable = true;
			var content = new DecimalContent(manager);
			cell.Content = content;
			var param = new Param(manager, content) { Name = "Сумма", Target = "Value" };
			content.AddParam(param);

			var control = new PresentControl();
			control.AttachManager(manager);

			Assert.IsTrue(control.BeginCellEdit(cell.Location));
			control.CellEditText = "abc";
			control.CommitCellEdit();

			Assert.IsTrue(control.IsCellEditing);
			Assert.IsNull(param.Value);
			Assert.IsFalse(control.CanUndoCellEdit);

			control.CellEditText = "12.5";
			control.CommitCellEdit();
			Assert.IsFalse(control.IsCellEditing);
			Assert.AreEqual(12.5m, Convert.ToDecimal(param.Value));
		}

		[TestMethod]
		public void DoubleClick_OnEditableCell_StartsEditing()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			var cell = CreateTextCell(manager, 0, 0, "text", editable: true);
			var control = new PresentControl();
			control.AttachManager(manager);
			Assert.IsNotNull(control.Presenter);

			var rect = control.Presenter.GetCellRect(cell.Location);
			control.Presenter.RaiseDblClick(control,
				EventArgs.Empty,
				rect.X + rect.Width / 2,
				rect.Y + rect.Height / 2);

			Assert.IsTrue(control.IsCellEditing);
		}

		[TestMethod]
		public void DoubleClick_OnCellWithAction_DoesNotStartEditing()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			var cell = manager.CreateCell(0, 0);
			cell.Editable = true;
			var collection = new CollectionContent(manager, 2);
			collection.Add(new TextContent(manager) { Text = "text" });
			collection.Add(new ActionContent(manager) { Type = ActionType.MouseDblClick });
			cell.Content = collection;

			var control = new PresentControl();
			control.AttachManager(manager);
			Assert.IsNotNull(control.Presenter);

			var rect = control.Presenter.GetCellRect(cell.Location);
			control.Presenter.RaiseDblClick(control,
				EventArgs.Empty,
				rect.X + rect.Width / 2,
				rect.Y + rect.Height / 2);

			Assert.IsFalse(control.IsCellEditing);
		}

		[TestMethod]
		public void MouseClick_ReachesCellControl_NotOverlay()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			manager.SetColumnWidth(0, 10);
			manager.SetColumnWidth(1, 10);
			manager.SetRowHeight(0, 6);
			manager.SetRowHeight(1, 6);
			CreateTextCell(manager, 0, 0, "a", editable: true);
			var target = CreateTextCell(manager, 1, 1, "b", editable: true);

			var control = new PresentControl { Width = 400, Height = 300 };
			control.AttachManager(manager);

			var window = new Window { Width = 400, Height = 300, Content = control };
			window.Show();

			var cellControl = control.GetVisualDescendants().OfType<CellControl>().First();
			var rect = control.Presenter!.GetCellRect(target.Location);
			var point = cellControl.TranslatePoint(
				new global::Avalonia.Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), window);
			Assert.IsTrue(point.HasValue);

			window.MouseDown(point!.Value, MouseButton.Left, RawInputModifiers.None);
			window.MouseUp(point!.Value, MouseButton.Left, RawInputModifiers.None);

			Assert.AreEqual(target.Location.Row, control.Presenter.CurrentCell.Row,
				"actual=" + control.Presenter.CurrentCell.Row + "," + control.Presenter.CurrentCell.Column);
			Assert.AreEqual(target.Location.Column, control.Presenter.CurrentCell.Column);
			window.Close();
		}

		[TestMethod]
		public void Editing_RepositionsOnScroll()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			manager.SetColumnWidth(0, 10);
			manager.SetColumnWidth(1, 10);
			manager.SetRowHeight(0, 6);
			var cell = CreateTextCell(manager, 0, 1, "text", editable: true);

			var control = new PresentControl { Width = 400, Height = 300 };
			control.AttachManager(manager);
			var window = new Window { Width = 400, Height = 300, Content = control };
			window.Show();

			Assert.IsTrue(control.BeginCellEdit(cell.Location));

			var textBox = control.GetVisualDescendants().OfType<TextBox>()
				.First(item => item.Name == "inlineEditor");
			var before = textBox.Margin;

			control.Presenter!.LeftColumn = 1;
			control.Presenter.Update();

			Assert.AreNotEqual(before, textBox.Margin);
			window.Close();
		}

		[TestMethod]
		public void TextInput_StartsEditingWithTypedText()
		{
			var manager = CreateManager(CellEditMode.MarkedCells);
			var cell = CreateTextCell(manager, 0, 0, "old", editable: true);
			var control = new PresentControl { Width = 400, Height = 300 };
			control.AttachManager(manager);

			var window = new Window { Width = 400, Height = 300, Content = control };
			window.Show();

			control.Presenter!.CurrentCell = cell.Location;
			var cellControl = control.GetVisualDescendants().OfType<CellControl>().First();
			cellControl.Focus();

			window.KeyTextInput("A");

			Assert.IsTrue(control.IsCellEditing);
			Assert.AreEqual("A", control.CellEditText);

			window.KeyTextInput("B");
			Assert.AreEqual("AB", control.CellEditText);
			window.Close();
		}

		private sealed class TestApplication : Application
		{
		}
	}
}
#endif
