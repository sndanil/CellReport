using CellReport.Core;
using CellReport.Export.Pdf;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class PrintPageImageBuilderTests
	{
		[TestMethod]
		public void DefaultPageSize_IsA4()
		{
			var builder = new PrintPageImageBuilder(new CellManager(1, 1), TestExportHelper.Renderer);

			var size = builder.GetPagePixelSize();

			Assert.AreEqual(794, size.Width);
			Assert.AreEqual(1123, size.Height);
		}

		[TestMethod]
		public void ExplicitPageSize_UsesPrinterPaper()
		{
			var builder = new PrintPageImageBuilder(
				new CellManager(1, 1),
				TestExportHelper.Renderer,
				null,
				88f,
				200f);

			var size = builder.GetPagePixelSize();

			Assert.AreEqual(333, size.Width, "88 мм при 96 dpi");
			Assert.AreEqual(756, size.Height, "200 мм при 96 dpi");
		}

		[TestMethod]
		public void ExplicitDpi_ScalesPagePixels()
		{
			var builder = new PrintPageImageBuilder(
				new CellManager(1, 1),
				TestExportHelper.Renderer,
				null,
				88f,
				200f,
				null,
				203f);

			var size = builder.GetPagePixelSize();

			Assert.AreEqual(704, size.Width, "88 мм при 203 dpi");
			Assert.AreEqual(1599, size.Height, "200 мм при 203 dpi");
		}

		[TestMethod]
		public void ExplicitMargins_AreUsedForPrintableBounds()
		{
			var builder = new PrintPageImageBuilder(
				new CellManager(1, 1),
				TestExportHelper.Renderer,
				null,
				100f,
				100f,
				new Margins(10f));

			var bounds = builder.GetPrintableBounds();

			Assert.AreEqual(10f / 25.4f * 96f, bounds.X, 0.01f);
			Assert.AreEqual(10f / 25.4f * 96f, bounds.Y, 0.01f);
			Assert.AreEqual(80f / 25.4f * 96f, bounds.Width, 0.01f);
			Assert.AreEqual(80f / 25.4f * 96f, bounds.Height, 0.01f);
		}
	}
}
