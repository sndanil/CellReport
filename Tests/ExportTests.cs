using CellReport;
using CellReport.Export;
using CellReport.Export.Excel;
using CellReport.Export.Html;
using CellReport.Export.Image;
using CellReport.Export.Pdf;
using CellReport.Presenters;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

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
		public void Image_Export_WithBarcode_ProducesPng()
		{
			var report = TestTemplateFactory.LoadReport(BarcodeTemplate);

			var data = TestExportHelper.ExportToBytes(
				new ImageExporter(report.Manager, TestExportHelper.Renderer));

			TestExportHelper.AssertPng(data);
		}

		[TestMethod]
		public void Image_Export_WithChart_ProducesPng()
		{
			var report = TestTemplateFactory.LoadReport(ChartTemplate);

			var data = TestExportHelper.ExportToBytes(
				new ImageExporter(report.Manager, TestExportHelper.Renderer));

			TestExportHelper.AssertPng(data);
		}

		[TestMethod]
		public async Task Image_Export_ParallelDocuments_ProducesIndependentPngs()
		{
			var first = CreateSampleReport();
			var second = CreateSampleReport();

			var exports = await Task.WhenAll(
				Task.Run(() => TestExportHelper.ExportToBytes(
					new ImageExporter(first.Manager, new CellReport.Presenters.Skia.SkiaReportRenderer()))),
				Task.Run(() => TestExportHelper.ExportToBytes(
					new ImageExporter(second.Manager, new CellReport.Presenters.Skia.SkiaReportRenderer()))))
				.ConfigureAwait(false);

			TestExportHelper.AssertPng(exports[0]);
			TestExportHelper.AssertPng(exports[1]);
			Assert.AreNotSame(exports[0], exports[1]);
		}

		[TestMethod]
		[DoNotParallelize]
		public void ImageExporter_WithoutRenderer_ThrowsClearException()
		{
			var previousFactory = BaseExporter.RendererFactory;
			try
			{
				BaseExporter.RendererFactory = new NullRendererFactory();
				var report = CreateSampleReport();

				Assert.ThrowsException<System.ArgumentNullException>(
					() => new ImageExporter(report.Manager, null));
			}
			finally
			{
				BaseExporter.RendererFactory = previousFactory;
			}
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

		private sealed class NullRendererFactory : IRendererFactory
		{
			public BaseRenderer Create()
			{
				return null;
			}
		}

		private const string BarcodeTemplate = @"<CellReportTemplate>
  <Properties DefaultRowHeight=""50"" DefaultColumnWidth=""100"" />
  <Table RowsCount=""1"" ColumnsCount=""1"">
    <Row Height=""50""><Cell><BarCodeContent Type=""QR"" Orientation=""Horizontal""><Code>CellReport</Code></BarCodeContent></Cell></Row>
  </Table>
  <Styles />
  <Expressions />
</CellReportTemplate>";

		private const string ChartTemplate = @"<CellReportTemplate>
  <Properties DefaultRowHeight=""60"" DefaultColumnWidth=""120"" />
  <Table RowsCount=""1"" ColumnsCount=""1"">
    <Row Height=""60""><Cell><ChartContent ShowLegend=""true"">
      <Series Name=""Sales"" Type=""Column"" XDataType=""System.String"" YDataType=""System.Decimal"">
        <Data><X>One</X><Y>1.5</Y></Data>
        <Data><X>Two</X><Y>2.5</Y></Data>
      </Series>
    </ChartContent></Cell></Row>
  </Table>
  <Styles />
  <Expressions />
</CellReportTemplate>";
	}
}
