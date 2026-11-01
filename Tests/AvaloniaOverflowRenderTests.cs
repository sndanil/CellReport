#if NET10_0
using Avalonia;
using Avalonia.Headless;
using CellReport.Avalonia;
using CellReport.Core;
using CellReport.Core.Contents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace CellReportTests
{
	[TestClass]
	[DoNotParallelize]
	public class AvaloniaOverflowRenderTests
	{
		private const int CellWidth = 40;
		private const int CellHeight = 24;
		private const float CellWidthMm = CellWidth * 25.4f / 96f;
		private const float CellHeightMm = CellHeight * 25.4f / 96f;

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			if (Application.Current == null)
			{
				AppBuilder.Configure<TestApplication>()
					.UseHeadless(new AvaloniaHeadlessPlatformOptions())
					.SetupWithoutStarting();
			}
		}

		private static CellManager CreateManager(int columns, TextOverflow overflow, int textColumn = 0)
		{
			var manager = new CellManager(1, columns);
			for (var column = 0; column < columns; column++)
				manager.SetColumnWidth(column, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);

			var style = new TextContentStyle(HorizontalAlignment.Left, VerticalAlignment.Middle, FontInfo.Default, "Black", overflow);
			style.ID = "overflow-style";
			manager.AddStyle(style);

			var cell = manager.CreateCell(0, textColumn);
			cell.Content = new TextContent(manager)
			{
				Text = "AAAAAAAAAAAAAAAAAAAA",
				StyleID = style.ID
			};

			return manager;
		}

		private static SKSurface Render(CellManager manager, int columns, int leftColumn = 0, int frozenColumns = 0)
		{
			manager.DisplaySettings.FrozenColumns = frozenColumns;
			manager.DisplaySettings.ShowCurrentRows = false;

			var presentControl = new PresentControl();
			var cellControl = new CellControl { Width = columns * CellWidth, Height = CellHeight };
			cellControl.Measure(new global::Avalonia.Size(cellControl.Width, cellControl.Height));
			cellControl.Arrange(new global::Avalonia.Rect(0, 0, cellControl.Width, cellControl.Height));
			Assert.IsTrue(cellControl.Bounds.Width > 0, "bounds=" + cellControl.Bounds);

			var presenter = new AvaloniaDisplayPresenter(manager, presentControl, cellControl);
			presenter.LeftColumn = leftColumn;

			var surface = SKSurface.Create(new SKImageInfo(columns * CellWidth, CellHeight));
			surface.Canvas.Clear(SKColors.White);
			presenter.Render(cellControl, surface.Canvas);
			return surface;
		}

		private static int CountTextPixels(SKSurface surface, int left, int right)
		{
			var count = 0;
			using (var image = surface.Snapshot())
			using (var pixels = image.PeekPixels())
			{
				for (var y = 0; y < CellHeight; y++)
				{
					for (var x = left; x < right; x++)
					{
						var pixel = pixels.GetPixelColor(x, y);
						if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100)
							count++;
					}
				}
			}

			return count;
		}

		[TestMethod]
		public void OverflowAuto_DrawsIntoEmptyNeighbourCell()
		{
			using (var surface = Render(CreateManager(3, TextOverflow.Auto), 3))
			{
				Assert.IsTrue(CountTextPixels(surface, CellWidth + 1, CellWidth * 2) > 0,
					"текст должен перетекать в пустую соседнюю ячейку");
			}
		}

		[TestMethod]
		public void OverflowClip_DoesNotLeaveCell()
		{
			using (var surface = Render(CreateManager(3, TextOverflow.Clip), 3))
			{
				Assert.IsTrue(CountTextPixels(surface, 1, CellWidth) > 0, "текст должен рисоваться в своей ячейке");
				Assert.AreEqual(0, CountTextPixels(surface, CellWidth + 1, CellWidth * 3),
					"при Clip текст не должен выходить за ячейку");
			}
		}

		[TestMethod]
		public void Overflow_StopsAtNonEmptyNeighbour()
		{
			var manager = CreateManager(3, TextOverflow.Auto);
			var neighbour = manager.CreateCell(0, 1);
			neighbour.Content = new ImageContent(manager);

			using (var surface = Render(manager, 3))
			{
				Assert.AreEqual(0, CountTextPixels(surface, CellWidth + 1, CellWidth * 3),
					"занятый сосед должен останавливать переполнение");
			}
		}

		[TestMethod]
		public void FrozenColumn_CutsOverflowAtPaneBoundary()
		{
			using (var surface = Render(CreateManager(3, TextOverflow.Auto), 3, 0, 1))
			{
				Assert.IsTrue(CountTextPixels(surface, 1, CellWidth) > 0, "в своей панели текст виден");
				Assert.AreEqual(0, CountTextPixels(surface, CellWidth + 1, CellWidth * 3),
					"переполнение не должно пересекать границу frozen-панели");
			}
		}

		[TestMethod]
		public void SeedCell_ScrolledOutOrigin_StillVisible()
		{
			using (var surface = Render(CreateManager(3, TextOverflow.Auto), 3, 1))
			{
				Assert.IsTrue(CountTextPixels(surface, CellWidth + 1, CellWidth * 3) > 0,
					"хвост текста из прокрученной ячейки должен быть виден");
			}
		}

		[TestMethod]
		public void VerticalSeed_ImageScrolledAbove_StillVisible()
		{
			var manager = new CellManager(3, 1);
			manager.SetColumnWidth(0, CellWidthMm);
			for (var row = 0; row < 3; row++)
				manager.SetRowHeight(row, CellHeightMm);
			manager.AddResource("img", CreateSolidPng(100, 100, SKColors.Red));

			var cell = manager.CreateCell(0, 0);
			cell.Content = new ImageContent(manager)
			{
				Uri = "RESOURCE://img",
				SizeMode = ImageSizeMode.Actual,
				Anchor = ImageAnchor.Center,
				Overflow = true
			};
			manager.DisplaySettings.ShowCurrentRows = false;

			Assert.IsTrue(manager.GetResource("img").Length > 0);
			Assert.IsTrue(((ImageContent)cell.Content).GetImageData().Length > 0, "ресурс изображения должен читаться");

			var presentControl = new PresentControl();
			var cellControl = new CellControl { Width = CellWidth, Height = CellHeight * 3 };
			cellControl.Measure(new global::Avalonia.Size(CellWidth, CellHeight * 3));
			cellControl.Arrange(new global::Avalonia.Rect(0, 0, CellWidth, CellHeight * 3));

			var presenter = new AvaloniaDisplayPresenter(manager, presentControl, cellControl);
			presenter.TopRow = 1;

			using (var surface = SKSurface.Create(new SKImageInfo(CellWidth, CellHeight * 3)))
			{
				surface.Canvas.Clear(SKColors.White);
				presenter.Render(cellControl, surface.Canvas);

				using (var image = surface.Snapshot())
				using (var pixels = image.PeekPixels())
				{
					var red = 0;
					for (var y = 0; y < CellHeight * 3; y++)
					{
						for (var x = 0; x < CellWidth; x++)
						{
							var pixel = pixels.GetPixelColor(x, y);
							if (pixel.Red > 200 && pixel.Green < 60 && pixel.Blue < 60)
								red++;
						}
					}

					Assert.IsTrue(red > 0, "изображение из прокрученной верхней строки должно быть видно");
				}
			}
		}

		private static byte[] CreateSolidPng(int width, int height, SKColor color)
		{
			using (var bitmap = new SKBitmap(width, height))
			using (var canvas = new SKCanvas(bitmap))
			{
				canvas.Clear(color);
				using (var image = SKImage.FromBitmap(bitmap))
				using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
					return data.ToArray();
			}
		}

		private sealed class TestApplication : Application
		{
		}
	}
}
#endif
