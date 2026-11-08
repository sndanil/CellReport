using System.Xml.Linq;
using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class PrintEngineTests
	{
		[TestMethod]
		public void MissingAttribute_DefaultsToGdi()
		{
			var settings = PrintSettings.Parse(new CellManager(1, 1), new XElement("PrintSettings"));

			Assert.AreEqual(PrintEngine.Gdi, settings.PrintEngine);
			Assert.IsNull(settings.Serialize().Attribute("PrintEngine"));
		}

		[TestMethod]
		public void SkiaAttribute_RoundTrips()
		{
			var settings = PrintSettings.Parse(
				new CellManager(1, 1),
				new XElement("PrintSettings", new XAttribute("PrintEngine", "Skia")));

			Assert.AreEqual(PrintEngine.Skia, settings.PrintEngine);
			Assert.AreEqual("Skia", settings.Serialize().Attribute("PrintEngine").Value);
		}

		[TestMethod]
		public void DevicePixels_UsePrinterDpiAndHundredths()
		{
			int x, y, width, height;
			PrintDeviceMap.ToDevicePixels(39f, 39f, 1091f, 749f, 1f, 600f, 600f, out x, out y, out width, out height);

			Assert.AreEqual(234, x);
			Assert.AreEqual(234, y);
			Assert.AreEqual(6546, width);
			Assert.AreEqual(4494, height);
		}

		[TestMethod]
		public void DevicePixels_ScaleWhenPageUnitsDifferFromPaper()
		{
			int x, y, width, height;
			PrintDeviceMap.ToDevicePixels(22f, 22f, 623f, 428f, 0.5714f, 600f, 600f, out x, out y, out width, out height);

			Assert.AreEqual(231, x);
			Assert.AreEqual(231, y);
			Assert.AreEqual(6542, width);
			Assert.AreEqual(4494, height);
		}

		[TestMethod]
		public void RasterDpi_IsCapped()
		{
			Assert.AreEqual(300f, PrintDeviceMap.CapRasterDpi(600f));
			Assert.AreEqual(96f, PrintDeviceMap.CapRasterDpi(0f));
		}
	}
}
