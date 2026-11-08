#if NET10_0_OR_GREATER
using CellReport.Avalonia.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class CupsPrinterQueryTests
	{
		private const string Printers = """
			printer Office_Laser is idle. enabled since Mon Nov 2 10:00:00 2026
			printer Receipt disabled since Mon Nov 2 09:00:00 2026
			system default destination: Office_Laser
			""";

		[TestMethod]
		public void ParsePrinters_ReadsNames()
		{
			var printers = CupsPrinterQuery.ParsePrinters(Printers);

			Assert.AreEqual(2, printers.Count);
			Assert.AreEqual("Office_Laser", printers[0]);
			Assert.AreEqual("Receipt", printers[1]);
		}

		[TestMethod]
		public void ParseDefaultPrinter_ReadsDestination()
		{
			Assert.AreEqual("Office_Laser", CupsPrinterQuery.ParseDefaultPrinter(Printers));
		}

		[TestMethod]
		public void ParseDefaultMedia_ReadsStarredOption()
		{
			const string options = "PageSize/Media Size: Letter *A4 Legal Custom.80x200mm\n";

			Assert.AreEqual("A4", CupsPrinterQuery.ParseDefaultMedia(options));
		}

		[TestMethod]
		public void PaperMillimetres_KnownCustomAndPoints()
		{
			float width, height;
			Assert.IsTrue(CupsPrinterQuery.TryGetPaperMillimetres("A4", out width, out height));
			Assert.AreEqual(210f, width);
			Assert.AreEqual(297f, height);

			Assert.IsTrue(CupsPrinterQuery.TryGetPaperMillimetres("Custom.80x200mm", out width, out height));
			Assert.AreEqual(80f, width);
			Assert.AreEqual(200f, height);

			Assert.IsTrue(CupsPrinterQuery.TryGetPaperMillimetres("w288h432", out width, out height));
			Assert.AreEqual(288f * 25.4f / 72f, width, 0.01f);
			Assert.AreEqual(432f * 25.4f / 72f, height, 0.01f);
		}
	}
}
#endif
