using System;
using System.IO;
using System.Linq;
using System.Text;
using CellReport;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class ReportDocumentTests
	{
		[TestMethod]
		public void JoinSection_RepeatsHeaderAndCellRows()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.HeaderAndCellSections);

			report.AddSection("header");
			report.SetParameter("header", "h111");
			report.JoinSection("header");
			report.SetParameter("header", "h222");
			report.JoinSection("header");
			report.SetParameter("header", "h333");

			report.AddSection("cell");
			report.SetParameter("cell", "c111");
			report.JoinSection("cell");
			report.SetParameter("cell", "c222");
			report.JoinSection("cell");
			report.SetParameter("cell", "c333");

			report.AddSection("cell");
			report.SetParameter("cell", "c211");
			report.JoinSection("cell");
			report.SetParameter("cell", "c222");
			report.JoinSection("cell");
			report.SetParameter("cell", "c233");

			Assert.AreEqual("h111", GetCellText(report.Manager, 0, 0));
			Assert.AreEqual("h222", GetCellText(report.Manager, 0, 1));
			Assert.AreEqual("h333", GetCellText(report.Manager, 0, 2));
			Assert.AreEqual("c111", GetCellText(report.Manager, 1, 0));
			Assert.AreEqual("c222", GetCellText(report.Manager, 1, 1));
			Assert.AreEqual("c333", GetCellText(report.Manager, 1, 2));
			Assert.AreEqual("c211", GetCellText(report.Manager, 2, 0));
		}

		[TestMethod]
		public void NamedObjectSection_CanBeResolved()
		{
			var template = new CellTemplate();
			template.Load(new MemoryStream(Encoding.UTF8.GetBytes(TestTemplateFactory.NamedObjectSection)));
			var report = new ReportDocument(template);

			report.AddSection("Секция 0");
			report.AddSection("Секция 0", "Test");

			var section = template.Manager.GetSection("Секция 0", "Товары");
			Assert.IsNotNull(section);
		}

		[TestMethod]
		public void SumExpression_AccumulatesDecimalValues()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.SumExpression);
			report.AddSection("Test");
			report.SetParameter("Количество", 1m);
			report.AddSection("Test");
			report.SetParameter("Количество", 2.5m);

			var expression = (SumExpression)report.Manager.GetExpressionByID("КоличествоВсего");
			Assert.AreEqual(3.5m, expression.GetValue(report.Manager));
		}

		[TestMethod]
		public void TemplatePackage_RoundTrip_PreservesRowCount()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.MultiCellRow);
			const int sectionCount = 50;

			for (int i = 0; i < sectionCount; i++)
			{
				report.AddSection("cell");
				for (int j = 1; j <= 6; j++)
					report.SetParameter("cell" + j, "v" + i + "_" + j);
			}

			var expectedRows = report.Manager.RowsCount;
			byte[] packageData;
			using (var stream = new MemoryStream())
			{
				new CellTemplate(report.Manager).Save(stream, TemplateType.Package);
				packageData = stream.ToArray();
			}

			Assert.IsTrue(packageData.Length > 0);

			var reloaded = new CellTemplate();
			reloaded.Load(new MemoryStream(packageData));

			Assert.AreEqual(expectedRows, reloaded.Manager.RowsCount);
			Assert.AreEqual(report.Manager.ColumnsCount, reloaded.Manager.ColumnsCount);
		}

		[TestMethod]
		public void ReportsFromSameTemplate_DoNotShareMutableSettingsOrStyles()
		{
			var template = new CellTemplate();
			template.Load(new MemoryStream(Encoding.UTF8.GetBytes(TestTemplateFactory.HeaderAndCellSections)));
			var first = new ReportDocument(template);
			var second = new ReportDocument(template);

			first.Culture = "ru-RU";
			first.Manager.DisplaySettings.Scale = 2f;
			first.Manager.ExportSettings.RunExcelAfterExport = false;
			var firstStyle = first.Manager.GetStyles().First();
			var secondStyle = second.Manager.GetStyles().First();
			firstStyle.ID = "changed";

			Assert.IsNull(second.Culture);
			Assert.AreEqual(1f, second.Manager.DisplaySettings.Scale);
			Assert.IsTrue(second.Manager.ExportSettings.RunExcelAfterExport);
			Assert.AreNotEqual("changed", secondStyle.ID);
			Assert.AreNotSame(first.Manager.Properties, second.Manager.Properties);
			Assert.AreNotSame(firstStyle, secondStyle);
		}

		private static string GetCellText(CellManager manager, int row, int column)
		{
			var cell = manager.GetCell(row, column);
			Assert.IsNotNull(cell);
			Assert.IsInstanceOfType(cell.Content, typeof(TextContent));
			return ((TextContent)cell.Content).GetText();
		}
	}
}
