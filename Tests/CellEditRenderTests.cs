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
	public class CellEditRenderTests
	{
		private const int CellWidth = 80;
		private const int CellHeight = 30;
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

		private static CellManager CreateManager(CellEditMode mode, string text, bool editable, string placeholder = null)
		{
			var manager = new CellManager(1, 1);
			manager.SetColumnWidth(0, CellWidthMm);
			manager.SetRowHeight(0, CellHeightMm);
			manager.DisplaySettings.ShowCurrentRows = false;
			manager.DisplaySettings.ShowGridLines = false;
			manager.EditSettings.Mode = mode;

			var cell = manager.CreateCell(0, 0);
			cell.Editable = editable;
			cell.EditPlaceholder = placeholder;
			cell.Content = new TextContent(manager) { Text = text };
			return manager;
		}

		private static SKSurface Render(CellManager manager)
		{
			var control = new PresentControl();
			var cellControl = new CellControl { Width = CellWidth, Height = CellHeight };
			cellControl.Measure(new global::Avalonia.Size(CellWidth, CellHeight));
			cellControl.Arrange(new Rect(0, 0, CellWidth, CellHeight));
			var presenter = new AvaloniaDisplayPresenter(manager, control, cellControl);

			var surface = SKSurface.Create(new SKImageInfo(CellWidth, CellHeight));
			surface.Canvas.Clear(SKColors.White);
			presenter.Render(cellControl, surface.Canvas);
			return surface;
		}

		private static int CountGreenPixels(SKSurface surface)
		{
			using var image = surface.Snapshot();
			using var bitmap = SKBitmap.FromImage(image);
			var count = 0;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					var color = bitmap.GetPixel(x, y);
					if (color.Green > color.Red + 20 && color.Green > color.Blue + 20)
						count++;
				}
			}

			return count;
		}

		private static int CountNonWhitePixels(SKSurface surface, int inset)
		{
			using var image = surface.Snapshot();
			using var bitmap = SKBitmap.FromImage(image);
			var count = 0;
			for (var y = inset; y < bitmap.Height - inset; y++)
			{
				for (var x = inset; x < bitmap.Width - inset; x++)
				{
					var color = bitmap.GetPixel(x, y);
					if (color.Red < 245 || color.Green < 245 || color.Blue < 245)
						count++;
				}
			}

			return count;
		}

		[TestMethod]
		public void MarkedCell_DrawsFrameOnlyInEditMode()
		{
			using (var surface = Render(CreateManager(CellEditMode.MarkedCells, "text", editable: true)))
				Assert.IsTrue(CountGreenPixels(surface) > 0, "рамка не нарисована");

			using (var surface = Render(CreateManager(CellEditMode.None, "text", editable: true)))
				Assert.AreEqual(0, CountGreenPixels(surface), "рамка нарисована в режиме только чтения");
		}

		[TestMethod]
		public void MarkedCell_DrawsPlaceholderOnlyWhenEmpty()
		{
			var manager = CreateManager(CellEditMode.MarkedCells, string.Empty, editable: true, placeholder: "Введите");
			manager.EditSettings.ShowFrame = false;

			using (var surface = Render(manager))
				Assert.IsTrue(CountNonWhitePixels(surface, 4) > 0, "плейсхолдер не нарисован");

			manager.GetCell(0, 0).Content = new TextContent(manager) { Text = "filled" };
			using (var surface = Render(manager))
				Assert.IsTrue(CountNonWhitePixels(surface, 4) > 0, "текст значения не нарисован");
		}

		[TestMethod]
		public void UnmarkedCell_DoesNotDrawFrame()
		{
			var manager = CreateManager(CellEditMode.MarkedCells, "text", editable: false);
			using var surface = Render(manager);
			Assert.AreEqual(0, CountGreenPixels(surface));
		}

		[TestMethod]
		public void PrintPages_DoNotContainEditHints()
		{
			var manager = CreateManager(CellEditMode.MarkedCells, string.Empty, editable: true, placeholder: "Введите");
			var presenter = new AvaloniaPrintPresenter(manager);
			presenter.BuildPages(80f, 50f);

			Assert.IsTrue(presenter.PageCount > 0);
			using var bitmap = SKBitmap.Decode(presenter.PageImages[0]);
			Assert.IsNotNull(bitmap);

			var green = 0;
			for (var y = 0; y < bitmap.Height; y++)
			{
				for (var x = 0; x < bitmap.Width; x++)
				{
					var color = bitmap.GetPixel(x, y);
					if (color.Green > color.Red + 20 && color.Green > color.Blue + 20)
						green++;
				}
			}

			Assert.AreEqual(0, green, "подсказки попали в печать");
		}

		private sealed class TestApplication : Application
		{
		}
	}
}
#endif
