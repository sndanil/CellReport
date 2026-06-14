using CellReport.Core;
using CellReport.Presenters.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class SkiaRendererTests
	{
		[TestMethod]
		public void GetTextSize_ReturnsNonZeroForText()
		{
			var rect = new Rectangle(0, 0, 200, 100);
			var size = SkiaReportRenderer.GetTextSize(rect, "Sample text", "Arial", 10, FontStyle.Regular);

			Assert.IsTrue(size.Width > 0);
			Assert.IsTrue(size.Height > 0);
		}

		[TestMethod]
		public void GetTextSize_EmptyText_ReturnsRectWidth()
		{
			var rect = new Rectangle(0, 0, 150, 80);
			var size = SkiaReportRenderer.GetTextSize(rect, string.Empty, "Arial", 10, FontStyle.Regular);

			Assert.AreEqual(150, size.Width);
			Assert.AreEqual(80, size.Height);
		}

		[TestMethod]
		public void GetTextSize_BoldText_IsNotSmallerThanRegular()
		{
			var rect = new Rectangle(0, 0, 300, 200);
			var regular = SkiaReportRenderer.GetTextSize(rect, "Bold compare", "Arial", 12, FontStyle.Regular);
			var bold = SkiaReportRenderer.GetTextSize(rect, "Bold compare", "Arial", 12, FontStyle.Bold);

			Assert.IsTrue(bold.Width >= regular.Width);
		}
	}
}
