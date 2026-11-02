using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Export.Excel;
using CellReport.Export.Image;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CellReportTests
{
	[TestClass]
	public class OverflowExportTests
	{
		private const int CellWidthPx = 40;
		private const int CellHeightPx = 24;
		private const float CellWidthMm = CellWidthPx * 25.4f / 96f;
		private const float CellHeightMm = CellHeightPx * 25.4f / 96f;

		private static CellManager CreateManager(TextOverflow overflow)
		{
			var manager = new CellManager(1, 3);
			for (var column = 0; column < 3; column++)
				manager.SetColumnWidth(column, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);

			var style = new TextContentStyle(HorizontalAlignment.Left, VerticalAlignment.Middle, FontInfo.Default, "Black", overflow);
			style.ID = "tc";
			manager.AddStyle(style);

			var cell = manager.CreateCell(0, 0);
			cell.Content = new TextContent(manager)
			{
				Text = "AAAAAAAAAAAAAAAAAAAA",
				StyleID = style.ID
			};

			return manager;
		}

		[TestMethod]
		public void Image_Export_OverflowsIntoEmptyNeighbour()
		{
			var data = TestExportHelper.ExportToBytes(new ImageExporter(CreateManager(TextOverflow.Auto), TestExportHelper.Renderer));

			using (var bitmap = SKBitmap.Decode(data))
			{
				Assert.IsNotNull(bitmap);
				var dark = 0;
				for (var y = 0; y < CellHeightPx; y++)
				{
					for (var x = CellWidthPx + 1; x < CellWidthPx * 2; x++)
					{
						var pixel = bitmap.GetPixel(x, y);
						if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100)
							dark++;
					}
				}

				Assert.IsTrue(dark > 0, "PNG-экспорт должен сохранять переполнение в пустую соседнюю ячейку");
			}
		}

		[TestMethod]
		public void Image_Export_ClipStaysInsideCell()
		{
			var data = TestExportHelper.ExportToBytes(new ImageExporter(CreateManager(TextOverflow.Clip), TestExportHelper.Renderer));

			using (var bitmap = SKBitmap.Decode(data))
			{
				var dark = 0;
				for (var y = 0; y < CellHeightPx; y++)
				{
					for (var x = CellWidthPx + 1; x < CellWidthPx * 2; x++)
					{
						var pixel = bitmap.GetPixel(x, y);
						if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100)
							dark++;
					}
				}

				Assert.AreEqual(0, dark);
			}
		}

		[TestMethod]
		public void Excel_Export_WritesNoWrapForOverflowStyle()
		{
			var auto = TestExportHelper.ExportToBytes(new ExcelExporter(CreateManager(TextOverflow.Auto), TestExportHelper.Renderer));
			var clip = TestExportHelper.ExportToBytes(new ExcelExporter(CreateManager(TextOverflow.Clip), TestExportHelper.Renderer));

			Assert.AreEqual("0", GetFirstWrapText(auto));
			Assert.AreEqual("1", GetFirstWrapText(clip));
		}

		[TestMethod]
		public void Image_Export_TextInNeighbourDrawnOverSpanningImage()
		{
			var manager = new CellManager(1, 3);
			for (var column = 0; column < 3; column++)
				manager.SetColumnWidth(column, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);

			var png = CreateRedPng();
			manager.AddResource("img", png);

			var imageCell = manager.CreateCell(0, 0);
			imageCell.Content = new ImageContent(manager)
			{
				Uri = "RESOURCE://img",
				SizeMode = ImageSizeMode.Stretch,
				ColSpan = 1,
				Overflow = true
			};

			var textCell = manager.CreateCell(0, 1);
			textCell.Content = new TextContent(manager) { Text = "TEXT" };

			var data = TestExportHelper.ExportToBytes(new ImageExporter(manager, TestExportHelper.Renderer));
			using (var bitmap = SKBitmap.Decode(data))
			{
				var text = 0;
				var red = 0;
				for (var y = 0; y < CellHeightPx; y++)
				{
					for (var x = CellWidthPx + 1; x < CellWidthPx * 2; x++)
					{
						var pixel = bitmap.GetPixel(x, y);
						if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100)
							text++;
						if (pixel.Red > 200 && pixel.Green < 60 && pixel.Blue < 60)
							red++;
					}
				}

				Assert.IsTrue(text > 0, "текст должен быть поверх картинки");
				Assert.IsTrue(red > 0, "картинка должна быть видна под текстом");
			}
		}

		private static byte[] CreateRedPng()
		{
			using (var bitmap = new SKBitmap(20, 20))
			using (var canvas = new SKCanvas(bitmap))
			{
				canvas.Clear(SKColors.Red);
				using (var image = SKImage.FromBitmap(bitmap))
				using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
					return data.ToArray();
			}
		}

		[TestMethod]
		public void Pdf_Page_DrawsTextOverSpanningImage()
		{
			var manager = new CellManager(1, 3);
			for (var column = 0; column < 3; column++)
				manager.SetColumnWidth(column, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);
			manager.AddResource("img", CreateRedPng());

			var imageCell = manager.CreateCell(0, 0);
			imageCell.Content = new ImageContent(manager)
			{
				Uri = "RESOURCE://img",
				SizeMode = ImageSizeMode.Stretch,
				ColSpan = 1,
				Overflow = true
			};
			var textCell = manager.CreateCell(0, 1);
			textCell.Content = new TextContent(manager) { Text = "TEXT" };

			var builder = new CellReport.Export.Pdf.PrintPageImageBuilder(
				manager, TestExportHelper.Renderer, null, 210f, 297f, new Margins(0));
			builder.BuildPages();
			Assert.IsTrue(builder.Pages.Count > 0);

			using (var bitmap = SKBitmap.Decode(builder.Pages[0]))
			{
				var text = 0;
				var red = 0;
				for (var y = 0; y < CellHeightPx; y++)
				{
					for (var x = CellWidthPx + 1; x < CellWidthPx * 2; x++)
					{
						var pixel = bitmap.GetPixel(x, y);
						if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100)
							text++;
						if (pixel.Red > 200 && pixel.Green < 60 && pixel.Blue < 60)
							red++;
					}
				}

				Assert.IsTrue(text > 0, "при печати текст должен быть поверх картинки");
				Assert.IsTrue(red > 0, "картинка должна быть видна под текстом");
			}
		}

		[TestMethod]
		public void Pdf_Page_RendersOverflowText()
		{
			var auto = CreatePrintBuilder(TextOverflow.Auto);
			auto.BuildPages();
			Assert.IsTrue(auto.Pages.Count > 0);

			var clip = CreatePrintBuilder(TextOverflow.Clip);
			clip.BuildPages();
			Assert.IsTrue(clip.Pages.Count > 0);

			Assert.IsTrue(CountDarkPixels(auto.Pages[0], CellWidthPx + 1, 300) > 0,
				"печать должна сохранять переполнение в пустую соседнюю ячейку; " +
				"0-40=" + CountDarkPixels(auto.Pages[0], 0, 40) +
				", 41-60=" + CountDarkPixels(auto.Pages[0], 41, 60) +
				", 61-100=" + CountDarkPixels(auto.Pages[0], 61, 100) +
				", 101-300=" + CountDarkPixels(auto.Pages[0], 101, 300));
			Assert.AreEqual(0, CountDarkPixels(clip.Pages[0], CellWidthPx + 1, 300),
				"при Clip печать не должна выходить за ячейку");
		}

		private static CellReport.Export.Pdf.PrintPageImageBuilder CreatePrintBuilder(TextOverflow overflow)
		{
			return new CellReport.Export.Pdf.PrintPageImageBuilder(
				CreateManager(overflow),
				TestExportHelper.Renderer,
				null,
				210f,
				297f,
				new Margins(0));
		}

		private static int CountDarkPixels(byte[] page, int left, int right)
		{
			using (var bitmap = SKBitmap.Decode(page))
			{
				var count = 0;
				var bottom = System.Math.Min(bitmap.Height, 400);
				for (var y = 0; y < bottom; y++)
				{
					for (var x = left; x < right && x < bitmap.Width; x++)
					{
						var pixel = bitmap.GetPixel(x, y);
						if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100)
							count++;
					}
				}

				return count;
			}
		}

		private static string GetFirstWrapText(byte[] package)
		{
			using (var stream = new MemoryStream(package))
			using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
			{
				var entry = archive.GetEntry("xl/styles.xml");
				Assert.IsNotNull(entry);
				using (var entryStream = entry.Open())
				{
					var document = XDocument.Load(entryStream);
					var ns = (XNamespace)"http://schemas.openxmlformats.org/spreadsheetml/2006/main";
					var alignment = document.Descendants(ns + "alignment").FirstOrDefault();
					Assert.IsNotNull(alignment);
					return (string)alignment.Attribute("wrapText");
				}
			}
		}
	}
}
