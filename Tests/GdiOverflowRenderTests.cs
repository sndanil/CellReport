#if NET48
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Presenters;
using CellReport.Presenters.WinForms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FontStyle = CellReport.Core.FontStyle;

namespace CellReportTests
{
	[TestClass]
	public class GdiOverflowRenderTests
	{
		private static int CountDarkPixels(Bitmap bitmap, int left, int right)
		{
			var count = 0;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = left; x < right; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100)
						count++;
				}
			}

			return count;
		}

		[TestMethod]
		public void RenderText_WithClipRect_DrawsInsideClipOnly()
		{
			var renderer = new GdiPlusRenderer();

			using (var bitmap = new Bitmap(120, 24))
			using (var graphics = Graphics.FromImage(bitmap))
			{
				graphics.Clear(Color.White);
				renderer.RenderText(graphics,
					new TextRenderOptions
					{
						LayoutRect = new CellReport.Core.Rectangle(0, 0, 140, 20),
						ClipRect = new CellReport.Core.Rectangle(0, 0, 120, 24),
						NoWrap = true
					},
					"AAAAAAAAAAAAAAAAAAAA", "Black",
					HorizontalAlignment.Left, VerticalAlignment.Middle, 0,
					"Arial", 10, FontStyle.Regular, Direction.LeftToRight);

				Assert.IsTrue(CountDarkPixels(bitmap, 0, 40) > 0, "текст должен рисоваться в своей ячейке");
				Assert.IsTrue(CountDarkPixels(bitmap, 41, 120) > 0, "без узкого клипа текст выходит за ячейку");
			}

			using (var bitmap = new Bitmap(120, 24))
			using (var graphics = Graphics.FromImage(bitmap))
			{
				graphics.Clear(Color.White);
				renderer.RenderText(graphics,
					new TextRenderOptions
					{
						LayoutRect = new CellReport.Core.Rectangle(0, 0, 140, 20),
						ClipRect = new CellReport.Core.Rectangle(0, 0, 40, 24),
						NoWrap = true
					},
					"AAAAAAAAAAAAAAAAAAAA", "Black",
					HorizontalAlignment.Left, VerticalAlignment.Middle, 0,
					"Arial", 10, FontStyle.Regular, Direction.LeftToRight);

				Assert.AreEqual(0, CountDarkPixels(bitmap, 41, 120), "текст не должен выходить за ClipRect");
			}
		}

		[TestMethod]
		public void MeasureText_ReturnsNaturalSingleLineWidth()
		{
			var renderer = new GdiPlusRenderer();
			var size = renderer.MeasureText("AAAAAAAAAAAAAAAAAAAA", "Arial", 10, FontStyle.Regular, 96);

			Assert.IsTrue(size.Width > 0);
			Assert.IsTrue(size.Height > 0);
		}

		[TestMethod]
		public void RenderImage_UsesRectAndClip()
		{
			var renderer = new GdiPlusRenderer();
			byte[] png;
			using (var source = new Bitmap(20, 20))
			{
				using (var graphics = Graphics.FromImage(source))
					graphics.Clear(Color.Red);
				using (var stream = new MemoryStream())
				{
					source.Save(stream, ImageFormat.Png);
					png = stream.ToArray();
				}
			}

			using (var bitmap = new Bitmap(120, 24))
			using (var graphics = Graphics.FromImage(bitmap))
			{
				graphics.Clear(Color.White);
				renderer.RenderImage(graphics,
					new ImageRenderOptions
					{
						CellRect = new CellReport.Core.Rectangle(0, 0, 40, 24),
						Rect = new CellReport.Core.Rectangle(-10, -10, 60, 60),
						ClipRect = new CellReport.Core.Rectangle(0, 0, 40, 24)
					},
					png);

				var inside = bitmap.GetPixel(10, 10);
				var outside = bitmap.GetPixel(45, 10);
				Assert.IsTrue(inside.R > 200 && inside.G < 50, "изображение должно рисоваться внутри ClipRect");
				Assert.IsTrue(outside.R > 200 && outside.G > 200 && outside.B > 200, "за ClipRect изображения быть не должно");
			}
		}
	}
}
#endif
