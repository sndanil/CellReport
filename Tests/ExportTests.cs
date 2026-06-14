using CellReport;
using CellReport.Export.Excel;
using CellReport.Export.Html;
using CellReport.Export.Image;
using CellReport.Export.Pdf;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class ExportTests
	{
		private static ReportDocument CreateSampleReport()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.SimpleTextRow);
			for (int i = 0; i < 5; i++)
			{
				report.AddSection("row");
				report.SetParameter("label", "Item " + i);
				report.SetParameter("value", (i * 10).ToString());
			}
			return report;
		}

		[TestMethod]
		public void Excel_Export_ProducesValidXlsx()
		{
			var report = CreateSampleReport();
			var data = TestExportHelper.ExportToBytes(new ExcelExporter(report.Manager, TestExportHelper.Renderer));
			TestExportHelper.AssertZipPackage(data, "xl/workbook.xml");
		}

		[TestMethod]
		public void Html_Export_ProducesHtmlDocument()
		{
			var report = CreateSampleReport();
			var data = TestExportHelper.ExportToBytes(new HtmlExporter(report.Manager, TestExportHelper.Renderer));
			TestExportHelper.AssertHtml(data);
		}

		[TestMethod]
		public void Pdf_Export_ProducesPdfHeader()
		{
			var report = CreateSampleReport();
			var exporter = PdfExporter.CreateVectorExporter(report.Manager);
			var data = TestExportHelper.ExportToBytes(exporter);
			TestExportHelper.AssertPdf(data);
		}

		[TestMethod]
		public void Image_Export_ProducesPng()
		{
			var report = CreateSampleReport();
			var data = TestExportHelper.ExportToBytes(new ImageExporter(report.Manager, TestExportHelper.Renderer));
			TestExportHelper.AssertPng(data);
		}

		[TestMethod]
		public void Excel_Export_SpecialCharacters_DoesNotThrow()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.SimpleTextRow);
			report.AddSection("row");
			report.SetParameter("label", "Error");
			report.SetParameter("value", "System.ArgumentException: \"\u0001\", hex 0x01 is invalid.");

			var data = TestExportHelper.ExportToBytes(new ExcelExporter(report.Manager, TestExportHelper.Renderer));
			TestExportHelper.AssertZipPackage(data, "xl/workbook.xml");
		}

		[TestMethod]
		public void Excel_Export_LargeReport_Completes()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.SimpleTextRow);
			for (int i = 0; i < 200; i++)
			{
				report.AddSection("row");
				report.SetParameter("label", "Row");
				report.SetParameter("value", i + 0.23m);
			}

			var data = TestExportHelper.ExportToBytes(new ExcelExporter(report.Manager, TestExportHelper.Renderer));
			TestExportHelper.AssertZipPackage(data, "xl/worksheets/sheet1.xml");
		}
	}
}
