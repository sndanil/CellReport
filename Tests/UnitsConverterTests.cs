using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class UnitsConverterTests
	{
		[TestMethod]
		public void PixelToWpfPoint_At96Dpi_IsIdentity()
		{
			Assert.AreEqual(100f, UnitsConverter.ConvertValue(100f, GraphicsUnit.Pixel, GraphicsUnit.WpfPoint, 96f));
		}

		[TestMethod]
		public void PixelToMillimeter_At96Dpi()
		{
			Assert.AreEqual(25.4f, UnitsConverter.ConvertValue(96f, GraphicsUnit.Pixel, GraphicsUnit.Millimeter, 96f), 0.001f);
		}

		[TestMethod]
		public void WpfPointToPixel_At96Dpi_IsIdentity()
		{
			Assert.AreEqual(96f, UnitsConverter.ConvertValue(96f, GraphicsUnit.WpfPoint, GraphicsUnit.Pixel, 96f));
		}

		[TestMethod]
		public void PixelToPoint_At96Dpi()
		{
			Assert.AreEqual(72f, UnitsConverter.ConvertValue(96f, GraphicsUnit.Pixel, GraphicsUnit.Point, 96f), 0.001f);
		}

		[TestMethod]
		public void PixelToPixel_KeepsValue()
		{
			Assert.AreEqual(42f, UnitsConverter.ConvertValue(42f, GraphicsUnit.Pixel, GraphicsUnit.Pixel, 96f));
		}
	}
}
