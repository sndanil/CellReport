using System;
using System.Globalization;
using System.Linq;
using CellReport.Core;
using CellReport.Core.Contents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class CellEditValueTests
	{
		private static CellManager CreateManager()
		{
			var manager = new CellManager(3, 3);
			manager.EditSettings.Mode = CellEditMode.AllCells;
			return manager;
		}

		private static TextContent AddTextContent(CellManager manager, Cell cell, string text)
		{
			var content = new TextContent(manager) { Text = text };
			cell.Content = content;
			return content;
		}

		private static Param AddParam(CellManager manager, BaseContent content, string name, string target, object value = null)
		{
			var param = new Param(manager, content) { Name = name, Target = target, Value = value };
			content.AddParam(param);
			return param;
		}

		[TestMethod]
		public void TextParam_TryGetAndSet()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = AddTextContent(manager, cell, null);
			var param = AddParam(manager, content, "ФИО", "Text", "Иванов");

			var editor = manager.CreateValueEditor(cell.Location);
			Assert.IsNotNull(editor);
			Assert.AreEqual(CellEditValueKind.Text, editor.Kind);

			string text;
			Assert.IsTrue(editor.TryGetText(out text));
			Assert.AreEqual("Иванов", text);

			CellEditError error;
			Assert.IsTrue(editor.TrySetText("Петров", out error));
			Assert.AreEqual(CellEditError.None, error);
			Assert.AreEqual("Петров", param.Value);
			Assert.AreEqual("Петров", content.GetText());
		}

		[TestMethod]
		public void SinglePlaceholder_WritesParamKeepsMarkupAndClearsVirtual()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = AddTextContent(manager, cell, "[ФИО]");
			var param = new Param(manager, content) { Name = "ФИО", IsVirtual = true };
			content.AddParam(param);

			var editor = manager.CreateValueEditor(cell.Location);
			Assert.IsNotNull(editor);
			string text;
			editor.TryGetText(out text);
			Assert.AreEqual(string.Empty, text);

			CellEditError error;
			Assert.IsTrue(editor.TrySetText("Иванов", out error));
			Assert.AreEqual("[ФИО]", content.Text);
			Assert.AreEqual("Иванов", param.Value);
			Assert.IsFalse(param.IsVirtual);
			Assert.AreEqual("Иванов", content.GetText());
		}

		[TestMethod]
		public void PlaceholderWithPrefix_WritesOnlyPlaceholderParam()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = AddTextContent(manager, cell, "Дата: [Дата]");
			var param = new Param(manager, content) { Name = "Дата", IsVirtual = true };
			content.AddParam(param);
			manager.EditSettings.Mode = CellEditMode.MarkedCells;
			cell.Editable = true;

			var editor = manager.CreateValueEditor(cell.Location);
			Assert.IsNotNull(editor);
			CellEditError error;
			Assert.IsTrue(editor.TrySetText("01.02.2026", out error));
			Assert.AreEqual("Дата: [Дата]", content.Text);
			Assert.AreEqual("01.02.2026", param.Value);
			Assert.AreEqual("Дата: 01.02.2026", content.GetText());
		}

		[TestMethod]
		public void LiteralText_IsEditedAsText()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = AddTextContent(manager, cell, "Примечание");

			var editor = manager.CreateValueEditor(cell.Location);
			string text;
			editor.TryGetText(out text);
			Assert.AreEqual("Примечание", text);

			CellEditError error;
			Assert.IsTrue(editor.TrySetText("Другое", out error));
			Assert.AreEqual("Другое", content.Text);
		}

		[TestMethod]
		public void MultiplePlaceholders_MarkedModeIsNotEditable_AllModeIsLiteral()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = AddTextContent(manager, cell, "[Фамилия] [Имя]");
			content.AddParam(new Param(manager, content) { Name = "Фамилия", IsVirtual = true });
			content.AddParam(new Param(manager, content) { Name = "Имя", IsVirtual = true });
			cell.Editable = true;

			manager.EditSettings.Mode = CellEditMode.MarkedCells;
			Assert.IsFalse(manager.IsCellEditable(cell.Location));
			Assert.AreEqual(CellEditError.NotEditable, manager.GetCellEditError(cell.Location));

			manager.EditSettings.Mode = CellEditMode.AllCells;
			Assert.IsTrue(manager.IsCellEditable(cell.Location));
			var editor = manager.CreateValueEditor(cell.Location);
			string text;
			editor.TryGetText(out text);
			Assert.AreEqual("[Фамилия] [Имя]", text);
		}

		[TestMethod]
		public void EditParam_BindsExistingParamByName()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = AddTextContent(manager, cell, null);
			AddParam(manager, content, "Первый", "Text", "1");
			var second = AddParam(manager, content, "Второй", "Text", "2");
			cell.EditParam = "Второй";
			cell.Editable = true;
			manager.EditSettings.Mode = CellEditMode.MarkedCells;

			var editor = manager.CreateValueEditor(cell.Location);
			string text;
			editor.TryGetText(out text);
			Assert.AreEqual("2", text);

			CellEditError error;
			Assert.IsTrue(editor.TrySetText("3", out error));
			Assert.AreEqual("3", second.Value);
			Assert.AreEqual("1", content.GetParams().First().Value);
		}

		[TestMethod]
		public void EmptyMarkedCell_CreatesTextContentAndParam()
		{
			var manager = new CellManager(2, 2);
			manager.EditSettings.Mode = CellEditMode.MarkedCells;
			var cell = manager.CreateCell(0, 0);
			cell.Editable = true;
			cell.EditParam = "ФИО";

			var editor = manager.CreateValueEditor(cell.Location);
			Assert.IsNotNull(editor);
			string text;
			editor.TryGetText(out text);
			Assert.AreEqual(string.Empty, text);

			CellEditError error;
			Assert.IsTrue(editor.TrySetText("Иванов", out error), "error=" + error);

			Assert.IsInstanceOfType(cell.Content, typeof(TextContent));
			var content = (TextContent)cell.Content;
			var param = content.GetParams().First();
			Assert.AreEqual("ФИО", param.Name);
			Assert.AreEqual("Text", param.Target);
			Assert.AreEqual("Иванов", param.Value);
			Assert.AreEqual("Иванов", content.GetText());
		}

		[TestMethod]
		public void EmptyCell_AllMode_CreatesTextContent()
		{
			var manager = new CellManager(2, 2);
			manager.EditSettings.Mode = CellEditMode.AllCells;

			var editor = manager.CreateValueEditor(new CellLocation { Row = 1, Column = 1 });
			Assert.IsNotNull(editor);
			CellEditError error;
			Assert.IsTrue(editor.TrySetText("Текст", out error));

			var cell = manager.GetCell(1, 1);
			Assert.IsNotNull(cell);
			Assert.IsInstanceOfType(cell.Content, typeof(TextContent));
			Assert.AreEqual("Текст", ((TextContent)cell.Content).Text);
		}

		[TestMethod]
		public void Decimal_ParsesWithParamCulture_AndRejectsInvalid()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = new DecimalContent(manager);
			cell.Content = content;
			var param = AddParam(manager, content, "Сумма", "Value");
			param.Culture = "ru-RU";

			var editor = manager.CreateValueEditor(cell.Location);
			Assert.AreEqual(CellEditValueKind.Decimal, editor.Kind);

			string text;
			editor.TryGetText(out text);
			Assert.AreEqual(string.Empty, text);

			CellEditError error;
			Assert.IsTrue(editor.TrySetText("1 234,50", out error), "error=" + error);
			Assert.AreEqual(1234.5m, Convert.ToDecimal(param.Value));
			Assert.AreEqual("Decimal", param.Type);

			Assert.IsFalse(editor.TrySetText("abc", out error));
			Assert.AreEqual(CellEditError.InvalidNumber, error);
		}

		[TestMethod]
		public void Date_ParsesAndRejectsInvalid()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = new DateTimeContent(manager);
			cell.Content = content;
			var param = AddParam(manager, content, "Дата", "Value");

			var editor = manager.CreateValueEditor(cell.Location);
			Assert.AreEqual(CellEditValueKind.DateTime, editor.Kind);

			string text;
			editor.TryGetText(out text);
			Assert.AreEqual(string.Empty, text);

			CellEditError error;
			Assert.IsTrue(editor.TrySetText("01.02.2026", out error), "error=" + error);
			Assert.AreEqual(new DateTime(2026, 2, 1), (DateTime)param.Value);
			Assert.AreEqual("DateTime", param.Type);

			Assert.IsFalse(editor.TrySetText("32.13.2026", out error));
			Assert.AreEqual(CellEditError.InvalidDate, error);
		}

		[TestMethod]
		public void TooLongValue_IsRejected()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			AddTextContent(manager, cell, "text");

			var editor = manager.CreateValueEditor(cell.Location);
			CellEditError error;
			Assert.IsFalse(editor.TrySetText(new string('x', 5000), out error));
			Assert.AreEqual(CellEditError.TooLong, error);
		}

		[TestMethod]
		public void UnsupportedContent_IsNotEditable()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			cell.Content = new ImageContent(manager);

			Assert.IsFalse(manager.IsCellEditable(cell.Location));
			Assert.AreEqual(CellEditError.UnsupportedContent, manager.GetCellEditError(cell.Location));
		}

		[TestMethod]
		public void EditParam_OnLiteralText_EditsLiteralText()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = AddTextContent(manager, cell, "Примечание");
			cell.EditParam = "Комментарий";
			cell.Editable = true;
			manager.EditSettings.Mode = CellEditMode.MarkedCells;

			var editor = manager.CreateValueEditor(cell.Location);
			Assert.IsNotNull(editor);
			CellEditError error;
			Assert.IsTrue(editor.TrySetText("Другое", out error));
			Assert.AreEqual("Другое", content.Text);
			Assert.AreEqual(0, content.GetParams().Count());
		}

		[TestMethod]
		public void Hint_FallsBackToParamNameAndPlaceholder()
		{
			var manager = CreateManager();
			var cell = manager.CreateCell(0, 0);
			var content = AddTextContent(manager, cell, null);
			AddParam(manager, content, "ФИО", "Text");

			var editor = manager.CreateValueEditor(cell.Location);
			Assert.AreEqual("ФИО", editor.Hint);
			StringAssert.Contains(editor.Tooltip, "ФИО");

			cell.EditPlaceholder = "Введите фамилию";
			cell.EditHint = "Поле обязательно";
			editor = manager.CreateValueEditor(cell.Location);
			Assert.AreEqual("Введите фамилию", editor.Hint);
			Assert.AreEqual("Поле обязательно", editor.Tooltip);
		}
	}
}
