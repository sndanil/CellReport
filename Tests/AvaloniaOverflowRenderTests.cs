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
		public void EditingCell_SuppressesOverflow()
		{
			var manager = CreateManager(3, TextOverflow.Auto);
			manager.DisplaySettings.ShowCurrentRows = false;

			var presentControl = new PresentControl();
			var cellControl = new CellControl { Width = CellWidth * 3, Height = CellHeight };
			cellControl.Measure(new global::Avalonia.Size(CellWidth * 3, CellHeight));
			cellControl.Arrange(new global::Avalonia.Rect(0, 0, CellWidth * 3, CellHeight));

			var presenter = new AvaloniaDisplayPresenter(manager, presentControl, cellControl);
			presenter.EditingCell = new CellLocation { Row = 0, Column = 0 };

			using (var surface = SKSurface.Create(new SKImageInfo(CellWidth * 3, CellHeight)))
			{
				surface.Canvas.Clear(SKColors.White);
				presenter.Render(cellControl, surface.Canvas);

				Assert.IsTrue(CountTextPixels(surface, 1, CellWidth) > 0, "текст редактируемой ячейки остаётся в ячейке");
				Assert.AreEqual(0, CountTextPixels(surface, CellWidth + 1, CellWidth * 3),
					"переполнение редактируемой ячейки скрывается");
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

		[TestMethod]
		public void ImageSpan_DrawsAcrossNeighbourCell()
		{
			var manager = new CellManager(1, 3);
			for (var column = 0; column < 3; column++)
				manager.SetColumnWidth(column, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);
			manager.AddResource("img", CreateSolidPng(20, 20, SKColors.Red));

			var cell = manager.CreateCell(0, 0);
			cell.Content = new ImageContent(manager)
			{
				Uri = "RESOURCE://img",
				SizeMode = ImageSizeMode.Stretch,
				ColSpan = 1,
				Overflow = true
			};
			manager.DisplaySettings.ShowCurrentRows = false;

			var presentControl = new PresentControl();
			var cellControl = new CellControl { Width = CellWidth * 3, Height = CellHeight };
			cellControl.Measure(new global::Avalonia.Size(CellWidth * 3, CellHeight));
			cellControl.Arrange(new global::Avalonia.Rect(0, 0, CellWidth * 3, CellHeight));

			var presenter = new AvaloniaDisplayPresenter(manager, presentControl, cellControl);
			using (var surface = SKSurface.Create(new SKImageInfo(CellWidth * 3, CellHeight)))
			{
				surface.Canvas.Clear(SKColors.White);
				presenter.Render(cellControl, surface.Canvas);

				using (var image = surface.Snapshot())
				using (var pixels = image.PeekPixels())
				{
					var red = 0;
					for (var y = 0; y < CellHeight; y++)
					{
						for (var x = CellWidth + 1; x < CellWidth * 2; x++)
						{
							var pixel = pixels.GetPixelColor(x, y);
							if (pixel.Red > 200 && pixel.Green < 60 && pixel.Blue < 60)
								red++;
						}
					}

					Assert.IsTrue(red > 0, "картинка с ColSpan=1 должна занимать соседнюю колонку");
				}
			}
		}

		[TestMethod]
		public void DesignMode_AutoParam_Renders()
		{
			var manager = CreateManager(3, TextOverflow.Clip);
			manager.IsDesignMode = true;
			var content = (TextContent)manager.GetCell(0, 0).Content;
			content.Text = string.Empty;
			content.AddParam(new Param(manager, content)
			{
				Name = "Текст",
				Target = "Text",
				Value = "qweqwe qweqweqwe qweq qwe eqwe qweqweqwe"
			});
			content.AddParam(new Param(manager, content)
			{
				Name = "Переполнение",
				Target = "Style.Overflow",
				Value = "Auto"
			});

			using (var surface = Render(manager, 3))
			{
				Assert.IsTrue(CountTextPixels(surface, 1, CellWidth * 3) > 0, "текст должен отображаться");
			}
		}

		[TestMethod]
		public void OverflowAuto_ScaledCanvas_Renders()
		{
			var manager = CreateManager(3, TextOverflow.Auto);
			manager.DisplaySettings.ShowCurrentRows = false;

			var presentControl = new PresentControl();
			var cellControl = new CellControl { Width = CellWidth * 3, Height = CellHeight };
			cellControl.Measure(new global::Avalonia.Size(CellWidth * 3, CellHeight));
			cellControl.Arrange(new global::Avalonia.Rect(0, 0, CellWidth * 3, CellHeight));

			var presenter = new AvaloniaDisplayPresenter(manager, presentControl, cellControl);

			using (var surface = SKSurface.Create(new SKImageInfo(CellWidth * 3 * 2, CellHeight * 2)))
			{
				surface.Canvas.Clear(SKColors.White);
				surface.Canvas.Scale(1.25f);
				presenter.Render(cellControl, surface.Canvas);

				using (var image = surface.Snapshot())
				using (var pixels = image.PeekPixels())
				{
					var dark = 0;
					for (var y = 0; y < CellHeight * 2; y++)
					{
						for (var x = 0; x < CellWidth * 3 * 2; x++)
						{
							var pixel = pixels.GetPixelColor(x, y);
							if (pixel.Red < 100 && pixel.Green < 100 && pixel.Blue < 100)
								dark++;
						}
					}

					Assert.IsTrue(dark > 0, "текст должен рисоваться при масштабированной канве");
				}
			}
		}

		[TestMethod]
		public void DefaultCenterStyle_AutoOverflow_Renders()
		{
			var manager = new CellManager(1, 3);
			for (var column = 0; column < 3; column++)
				manager.SetColumnWidth(column, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);
			manager.IsDesignMode = true;

			var cell = manager.CreateCell(0, 0);
			var content = new TextContent(manager) { Text = "qweqwe qweqweqwe qweq qwe eqwe qweqweqwe" };
			content.AddParam(new Param(manager, content)
			{
				Name = "Переполнение",
				Target = "Style.Overflow",
				Value = "Auto"
			});
			cell.Content = content;
			manager.DisplaySettings.ShowCurrentRows = false;

			var presentControl = new PresentControl();
			var cellControl = new CellControl { Width = CellWidth * 3, Height = CellHeight };
			cellControl.Measure(new global::Avalonia.Size(CellWidth * 3, CellHeight));
			cellControl.Arrange(new global::Avalonia.Rect(0, 0, CellWidth * 3, CellHeight));

			var presenter = new AvaloniaDisplayPresenter(manager, presentControl, cellControl);
			using (var surface = SKSurface.Create(new SKImageInfo(CellWidth * 3, CellHeight)))
			{
				surface.Canvas.Clear(SKColors.White);
				presenter.Render(cellControl, surface.Canvas);

				Assert.IsTrue(CountTextPixels(surface, 0, CellWidth * 3) > 0, "текст должен отображаться");
			}
		}

		[TestMethod]
		public void TextInNeighbourCell_DrawnOverSpanningImage()
		{
			var manager = new CellManager(1, 3);
			for (var column = 0; column < 3; column++)
				manager.SetColumnWidth(column, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);
			manager.AddResource("img", CreateSolidPng(20, 20, SKColors.Red));

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
			manager.DisplaySettings.ShowCurrentRows = false;

			using (var surface = Render(manager, 3))
			{
				Assert.IsTrue(CountTextPixels(surface, CellWidth + 1, CellWidth * 2) > 0,
					"текст соседней ячейки должен быть поверх картинки");
				Assert.IsTrue(CountRedPixels(surface, CellWidth + 1, CellWidth * 2) > 0,
					"картинка при этом должна быть видна");
			}
		}

		[TestMethod]
		public void TextAndImageInSameCell_TextDrawnOverImage()
		{
			var manager = new CellManager(1, 3);
			for (var column = 0; column < 3; column++)
				manager.SetColumnWidth(column, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);
			manager.AddResource("img", CreateSolidPng(20, 20, SKColors.Red));

			var cell = manager.CreateCell(0, 0);
			var collection = new CollectionContent(manager, 2);
			collection.Add(new ImageContent(manager)
			{
				Uri = "RESOURCE://img",
				SizeMode = ImageSizeMode.Stretch
			});
			collection.Add(new TextContent(manager) { Text = "TEXT" });
			cell.Content = collection;
			manager.DisplaySettings.ShowCurrentRows = false;

			using (var surface = Render(manager, 3))
			{
				Assert.IsTrue(CountTextPixels(surface, 1, CellWidth) > 0, "текст должен быть поверх картинки в своей ячейке");
				Assert.IsTrue(CountRedPixels(surface, 1, CellWidth) > 0, "картинка должна быть видна под текстом");
			}
		}

		private static int CountRedPixels(SKSurface surface, int left, int right)
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
						if (pixel.Red > 200 && pixel.Green < 60 && pixel.Blue < 60)
							count++;
					}
				}
			}

			return count;
		}

		private sealed class TestApplication : Application
		{
		}
	}
}
#endif
