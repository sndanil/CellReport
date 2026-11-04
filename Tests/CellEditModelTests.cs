using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CellReport;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class CellEditModelTests
	{
		private static CellManager CreateManager()
		{
			var manager = new CellManager(3, 4);
			manager.SetRowHeight(0, 8);
			manager.SetColumnWidth(0, 20);
			return manager;
		}

		private static Cell CreateTextCell(CellManager manager, int row, int column, string text,
			string paramName = null, string target = null, object value = null)
		{
			var cell = manager.CreateCell(row, column);
			var content = new TextContent(manager) { Text = text };
			cell.Content = content;
			if (paramName != null)
			{
				var param = new Param(manager, content) { Name = paramName, Target = target };
				if (value != null)
					param.Value = value;
				content.AddParam(param);
			}

			return cell;
		}

		[TestMethod]
		public void DefaultMode_IsReadOnly()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "text");
			cell.Editable = true;

			Assert.AreEqual(CellEditMode.None, manager.EditSettings.Mode);
			Assert.IsFalse(manager.IsCellEditable(cell.Location));
			Assert.IsNull(manager.CreateValueEditor(cell.Location));
		}

		[TestMethod]
		public void MarkedMode_EditsOnlyMarkedCells()
		{
			var manager = CreateManager();
			var marked = CreateTextCell(manager, 0, 0, "marked");
			var plain = CreateTextCell(manager, 0, 1, "plain");
			marked.Editable = true;
			manager.EditSettings.Mode = CellEditMode.MarkedCells;

			Assert.IsTrue(manager.IsCellEditable(marked.Location));
			Assert.IsFalse(manager.IsCellEditable(plain.Location));
		}

		[TestMethod]
		public void AllMode_EditsUnmarkedTextCell()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "text");
			manager.EditSettings.Mode = CellEditMode.AllCells;

			Assert.IsTrue(manager.IsCellEditable(cell.Location));
			var editor = manager.CreateValueEditor(cell.Location);
			Assert.IsNotNull(editor);
			Assert.AreEqual(CellEditValueKind.Text, editor.Kind);
		}

		[TestMethod]
		public void ComputedParam_IsNotEditable()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, null, "Сумма", "Text");
			var content = (TextContent)cell.Content;
			var param = content.GetParams().First();
			param.ExpressionID = "SumExpression";
			cell.Editable = true;
			manager.EditSettings.Mode = CellEditMode.MarkedCells;

			Assert.IsFalse(manager.IsCellEditable(cell.Location));
			Assert.AreEqual(CellEditError.Computed, manager.GetCellEditError(cell.Location));
		}

		[TestMethod]
		public void CellFlags_SerializeAndParse()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "text");
			cell.Editable = true;
			cell.EditParam = "ФИО";
			cell.EditPlaceholder = "Фамилия";
			cell.EditHint = "Подсказка";

			var element = XElement.Parse(cell.Serialize().ToString());
			var otherManager = CreateManager();
			var parsed = Cell.Parse(otherManager, element);

			Assert.IsTrue(parsed.Editable);
			Assert.AreEqual("ФИО", parsed.EditParam);
			Assert.AreEqual("Фамилия", parsed.EditPlaceholder);
			Assert.AreEqual("Подсказка", parsed.EditHint);
		}

		[TestMethod]
		public void CellFlags_NotSerializedWhenDefault()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "text");
			var xml = cell.Serialize().ToString();

			Assert.IsFalse(xml.Contains("Editable"));
			Assert.IsFalse(xml.Contains("EditParam"));
			Assert.IsFalse(xml.Contains("EditPlaceholder"));
			Assert.IsFalse(xml.Contains("EditHint"));
		}

		[TestMethod]
		public void OldTemplateWithoutFlags_LoadsReadOnly()
		{
			var template = new CellTemplate();
			template.LoadFromXml(
				"<CellReportTemplate><Properties/><Table RowsCount=\"1\" ColumnsCount=\"1\">"
				+ "<Row><Cell><TextContent><Text>hello</Text></TextContent></Cell></Row>"
				+ "</Table></CellReportTemplate>");

			var cell = template.Manager.GetCell(0, 0);
			Assert.IsNotNull(cell);
			Assert.IsFalse(cell.Editable);
			Assert.IsNull(cell.EditParam);
		}

		[TestMethod]
		public void TemplateWithFlags_RoundTripsThroughPackage()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, "text");
			cell.Editable = true;
			cell.EditParam = "ФИО";
			cell.EditPlaceholder = "Фамилия";

			byte[] data;
			using (var stream = new System.IO.MemoryStream())
			{
				new CellTemplate(manager).Save(stream, TemplateType.Package);
				data = stream.ToArray();
			}

			var reloaded = new CellTemplate();
			reloaded.Load(new System.IO.MemoryStream(data));
			var reloadedCell = reloaded.Manager.GetCell(0, 0);
			Assert.IsTrue(reloadedCell.Editable);
			Assert.AreEqual("ФИО", reloadedCell.EditParam);
			Assert.AreEqual("Фамилия", reloadedCell.EditPlaceholder);
		}

		[TestMethod]
		public void CreateCellFrom_CopiesEditFlags()
		{
			var manager = CreateManager();
			var source = CreateTextCell(manager, 0, 0, "text");
			source.Editable = true;
			source.EditParam = "ФИО";
			source.EditPlaceholder = "Фамилия";
			source.EditHint = "hint";

			var copy = manager.CreateCell(1, 1, source);

			Assert.IsTrue(copy.Editable);
			Assert.AreEqual("ФИО", copy.EditParam);
			Assert.AreEqual("Фамилия", copy.EditPlaceholder);
			Assert.AreEqual("hint", copy.EditHint);
		}

		[TestMethod]
		public void ReportDocument_SetParameterEditable_MarksCellAndMode()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.HeaderAndCellSections);
			report.AddSection("header");
			report.SetParameter("header", "value", editable: true);

			var cell = report.Manager.GetCell(0, 0);
			Assert.IsNotNull(cell);
			Assert.IsTrue(cell.Editable);
			Assert.AreEqual("header", cell.EditParam);
			Assert.AreEqual(CellEditMode.MarkedCells, report.Manager.EditSettings.Mode);
		}

		[TestMethod]
		public void ReportDocument_EnableCellEditing_ByName()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.HeaderAndCellSections);
			report.AddSection("cell");
			report.EnableCellEditing("cell");

			var cell = report.Manager.GetCell(0, 0);
			Assert.IsNotNull(cell);
			Assert.IsTrue(cell.Editable);
			Assert.AreEqual("cell", cell.EditParam);
			Assert.AreEqual(CellEditMode.MarkedCells, report.Manager.EditSettings.Mode);
		}

		[TestMethod]
		public void ReportDocument_EnableCellEditing_All()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.HeaderAndCellSections);
			report.AddSection("header");
			report.EnableCellEditing();

			Assert.AreEqual(CellEditMode.AllCells, report.Manager.EditSettings.Mode);
		}

		[TestMethod]
		public void AddSection_CopiesEditFlags()
		{
			var template = new CellTemplate();
			template.Load(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(TestTemplateFactory.HeaderAndCellSections)));
			template.Manager.GetCell(0, 0).Editable = true;
			template.Manager.GetCell(0, 0).EditParam = "header";
			var report = new ReportDocument(template);

			report.AddSection("header");
			report.JoinSection("header");

			var cell = report.Manager.GetCell(0, 1);
			Assert.IsNotNull(cell);
			Assert.IsTrue(cell.Editable);
			Assert.AreEqual("header", cell.EditParam);
		}

		[TestMethod]
		public void SetEditableByParam_MarksAllMatchingCells()
		{
			var manager = CreateManager();
			CreateTextCell(manager, 0, 0, null, "ФИО", "Text");
			CreateTextCell(manager, 0, 1, null, "ФИО", "Text");
			CreateTextCell(manager, 0, 2, null, "Другое", "Text");

			var count = manager.SetEditableByParam("ФИО", true, enableMarkedMode: true);

			Assert.AreEqual(2, count);
			Assert.IsTrue(manager.GetCell(0, 0).Editable);
			Assert.IsTrue(manager.GetCell(0, 1).Editable);
			Assert.IsFalse(manager.GetCell(0, 2).Editable);
			Assert.AreEqual(CellEditMode.MarkedCells, manager.EditSettings.Mode);
		}

		[TestMethod]
		public void FindNextEditable_MarkedMode_MovesForwardAndBackward()
		{
			var manager = CreateManager();
			var first = CreateTextCell(manager, 0, 0, null, "A", "Text");
			var second = CreateTextCell(manager, 1, 2, null, "B", "Text");
			first.Editable = true;
			second.Editable = true;
			manager.EditSettings.Mode = CellEditMode.MarkedCells;

			Assert.AreEqual(second.Location, manager.FindNextEditable(first.Location, true));
			Assert.AreEqual(first.Location, manager.FindNextEditable(second.Location, false));
			Assert.AreEqual(CellLocation.None, manager.FindNextEditable(second.Location, true));
		}

		[TestMethod]
		public void EditParam_WithDecimalType_WritesDecimal()
		{
			var manager = CreateManager();
			var cell = CreateTextCell(manager, 0, 0, null, "Сумма", "Text");
			cell.EditParam = "Сумма";
			cell.Editable = true;
			manager.EditSettings.Mode = CellEditMode.MarkedCells;

			var content = (TextContent)cell.Content;
			var param = content.GetParams().First();
			param.Type = "Decimal";
			param.Culture = "en-US";

			var editor = manager.CreateValueEditor(cell.Location);
			CellEditError error;
			Assert.IsTrue(editor.TrySetText("12.5", out error), "error=" + error);
			Assert.IsFalse(content.GetText().Length == 0);
			Assert.AreEqual(12.5m, Convert.ToDecimal(content.GetParams().First().Value));
		}
	}
}
