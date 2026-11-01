using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Presenters;
using CellReport.Presenters.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

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
		public void GetTextSize_ScalesWithDpi()
		{
			var rect = new Rectangle(0, 0, 5000, 1000);
			var at96 = SkiaReportRenderer.GetTextSize(rect, "Sample text", "Arial", 12, FontStyle.Regular, 96);
			var at203 = SkiaReportRenderer.GetTextSize(rect, "Sample text", "Arial", 12, FontStyle.Regular, 203);

			Assert.IsTrue(at203.Height > at96.Height * 1.9f, "текст должен масштабироваться под DPI принтера");
			Assert.IsTrue(at203.Height < at96.Height * 2.3f);
		}

		[TestMethod]
		public void GetTextSize_BoldText_IsNotSmallerThanRegular()
		{
			var rect = new Rectangle(0, 0, 300, 200);
			var regular = SkiaReportRenderer.GetTextSize(rect, "Bold compare", "Arial", 12, FontStyle.Regular);
			var bold = SkiaReportRenderer.GetTextSize(rect, "Bold compare", "Arial", 12, FontStyle.Bold);

			Assert.IsTrue(bold.Width >= regular.Width);
		}

		[TestMethod]
		public void RenderFillRect_WithoutInversion_KeepsOriginalColor()
		{
			Assert.AreEqual(SKColors.Black, RenderFillAndSample(new SKColor(0, 0, 0), false));
		}

		[TestMethod]
		public void RenderFillRect_WithInversion_InvertsColor()
		{
			Assert.AreEqual(SKColors.White, RenderFillAndSample(new SKColor(0, 0, 0), true));
			Assert.AreEqual(new SKColor(255, 255, 0), RenderFillAndSample(new SKColor(0, 0, 255), true));
		}

		[TestMethod]
		public void RenderText_WithClipRect_DrawsInsideClipOnly()
		{
			var renderer = new SkiaReportRenderer();
			using (var surface = SKSurface.Create(new SKImageInfo(200, 30)))
			{
				surface.Canvas.Clear(SKColors.White);
				renderer.RenderText(surface.Canvas,
					new TextRenderOptions
					{
						LayoutRect = new Rectangle(0, 0, 200, 20),
						ClipRect = new Rectangle(0, 0, 100, 30),
						NoWrap = true
					},
					"AAAAAAAAAAAAAAAAAAAA", "Black",
					HorizontalAlignment.Left, VerticalAlignment.Middle, 0,
					"Arial", 10, FontStyle.Regular, Direction.LeftToRight);

				using (var image = surface.Snapshot())
				using (var pixels = image.PeekPixels())
				{
					var inside = 0;
					var outside = 0;
					for (var y = 0; y < 30; y++)
					{
						for (var x = 0; x < 200; x++)
						{
							var pixel = pixels.GetPixelColor(x, y);
							if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100)
							{
								if (x < 100) inside++;
								else outside++;
							}
						}
					}

					Assert.IsTrue(inside > 0, "текст должен рисоваться внутри ClipRect");
					Assert.AreEqual(0, outside, "текст не должен выходить за ClipRect");
				}
			}
		}

		private static SKColor RenderFillAndSample(SKColor color, bool invertColors)
		{
			var renderer = new SkiaReportRenderer { InvertColors = invertColors };
			using (var surface = SKSurface.Create(new SKImageInfo(10, 10)))
			{
				surface.Canvas.Clear(SKColors.Transparent);
				renderer.RenderFillRect(surface.Canvas,
					new Rectangle(0, 0, 10, 10), 0,
					new[] { string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.Alpha, color.Red, color.Green, color.Blue) });

				using (var bitmap = surface.Snapshot())
				using (var snapshot = bitmap.PeekPixels())
				{
					return snapshot.GetPixelColor(5, 5);
				}
			}
		}
	}
}
