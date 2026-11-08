using CellReport.Avalonia;
using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace CellReport.Avalonia.Services
{
	internal static class WindowsPrintHelper
	{
		public static void Print(AvaloniaPrintPresenter presenter, bool useCustom, string? printerName, int fromPage = 1, int toPage = int.MaxValue)
		{
			new PrintOperation(presenter, fromPage, toPage).Run(useCustom, printerName);
		}

		private sealed class PrintOperation
		{
			private readonly AvaloniaPrintPresenter _presenter;
			private readonly CellManager _manager;
			private readonly int _requestedFromPage;
			private readonly int _requestedToPage;
			private IReadOnlyList<byte[]> _pages = Array.Empty<byte[]>();
			private RectangleF _drawable;
			private float _unitsPerHundredth = 1f;
			private int _fromPage;
			private int _toPage;
			private int _pageIndex;

			public PrintOperation(AvaloniaPrintPresenter presenter, int fromPage, int toPage)
			{
				_presenter = presenter;
				_manager = presenter.Manager;
				_requestedFromPage = Math.Max(1, fromPage);
				_requestedToPage = toPage <= 0 ? int.MaxValue : toPage;
			}

			public void Run(bool useCustom, string? printerName)
			{
				using var document = CreateDocument();
				if (!string.IsNullOrEmpty(printerName) &&
				    PrinterSettings.InstalledPrinters.Cast<string>().Any(p => p == printerName))
				{
					document.PrinterSettings.PrinterName = printerName;
				}

				if (useCustom && _requestedToPage != int.MaxValue)
				{
					document.PrinterSettings.PrintRange = PrintRange.SomePages;
					document.PrinterSettings.FromPage = _requestedFromPage;
					document.PrinterSettings.ToPage = _requestedToPage;
				}

				// Страницы строятся не здесь, а в первом PrintPage: только там
				// доступны фактические PageSettings и MarginBounds драйвера.
				document.Print();
			}

			private PrintDocument CreateDocument()
			{
				var reportMargins = _manager.PrintSettings.Margins ?? new CellReport.Core.Margins(0);
				var document = new PrintDocument
				{
					DocumentName = _manager.Properties.DocumentName,
					DefaultPageSettings =
					{
						Landscape = _manager.PrintSettings.Landscape,
						Margins = new System.Drawing.Printing.Margins(
							MillimetersToHundredthsInch(reportMargins.Left),
							MillimetersToHundredthsInch(reportMargins.Right),
							MillimetersToHundredthsInch(reportMargins.Top),
							MillimetersToHundredthsInch(reportMargins.Bottom))
					}
				};
				document.PrinterSettings.Copies = _manager.PrintSettings.Copies;
				document.PrintPage += OnPrintPage;
				return document;
			}

			private static RectangleF GetDrawableBounds(PrintPageEventArgs e)
			{
				// Не менять Graphics.PageUnit: это сбрасывает PageScale и
				// трансформацию, которую контроллер печати уже выставил под лист.
				var destination = new RectangleF(
					e.MarginBounds.X - e.PageSettings.HardMarginX,
					e.MarginBounds.Y - e.PageSettings.HardMarginY,
					e.MarginBounds.Width,
					e.MarginBounds.Height);

				var clip = e.Graphics!.VisibleClipBounds;
				if (clip.Width > 1 && clip.Height > 1 && SameSpace(clip, destination))
				{
					var visible = RectangleF.Intersect(destination, clip);
					if (visible.Width > 1 && visible.Height > 1)
						destination = visible;
				}

				return destination;
			}

			private static bool SameSpace(RectangleF clip, RectangleF destination)
			{
				var clipMax = Math.Max(clip.Width, clip.Height);
				var destMax = Math.Max(destination.Width, destination.Height);
				if (clipMax < 1 || destMax < 1)
					return false;

				// Клип в пикселях и поля в сотых дюйма отличаются в разы.
				var ratio = clipMax / destMax;
				return ratio > 0.33f && ratio < 3f;
			}

			/// <summary>
			/// Сколько текущих единиц Graphics приходится на одну сотую дюйма.
			/// У проверенных драйверов PageBounds и PaperSize в одном масштабе, и коэффициент равен 1.
			/// Если драйвер отдаст PageBounds в других единицах, размер страницы всё равно сойдётся с бумагой.
			/// </summary>
			private static float GraphicsUnitsPerHundredthInch(PrintPageEventArgs e)
			{
				var paper = e.PageSettings.PaperSize;
				if (paper == null || paper.Width <= 0 || paper.Height <= 0 || e.PageBounds.Width <= 0)
					return 1f;

				var paperWidth = paper.Width;
				var pageIsLandscape = e.PageBounds.Width > e.PageBounds.Height;
				var paperIsLandscape = paper.Width > paper.Height;
				if (pageIsLandscape != paperIsLandscape)
					paperWidth = paper.Height;

				var units = e.PageBounds.Width / (float)paperWidth;
				return units > 0.05f ? units : 1f;
			}

			private static int MillimetersToHundredthsInch(float millimeters)
			{
				return (int)Math.Round(millimeters * 100f / 25.4f);
			}

			private void EnsurePages(PrintPageEventArgs e)
			{
				if (_pages.Count > 0)
					return;

				// PageUnit не меняем: присваивание сбрасывает масштаб драйвера,
				// и лист рисуется крупнее бумаги.
				_drawable = GetDrawableBounds(e);
				var units = GraphicsUnitsPerHundredthInch(e);
				_unitsPerHundredth = units;
				var widthMm = Math.Max(1f, _drawable.Width / units * 25.4f / 100f);
				var heightMm = Math.Max(1f, _drawable.Height / units * 25.4f / 100f);
				var dpi = PrintDeviceMap.CapRasterDpi(Math.Max(e.Graphics!.DpiX, e.Graphics.DpiY));

				// Поля уже вычтены из MarginBounds, поэтому построителю
				// передаются нулевые поля.
				_presenter.BuildPages(widthMm, heightMm, dpi, new CellReport.Core.Margins(0));
				_pages = _presenter.PageImages;

				_fromPage = Math.Min(Math.Max(1, _requestedFromPage), Math.Max(1, _pages.Count));
				_toPage = _pages.Count == 0
					? 0
					: Math.Min(_pages.Count, Math.Max(_requestedToPage, _fromPage));
			}

			private void OnPrintPage(object sender, PrintPageEventArgs e)
			{
				EnsurePages(e);

				var currentPageNumber = _pageIndex + 1;
				if (currentPageNumber < _fromPage)
				{
					_pageIndex++;
					e.HasMorePages = _pageIndex < _pages.Count;
					return;
				}

				if (currentPageNumber > _toPage || _pages.Count == 0)
				{
					e.HasMorePages = false;
					return;
				}

				using var stream = new MemoryStream(_pages[_pageIndex]);
				using var image = Image.FromStream(stream);
				// GDI+ DrawImage в PerMonitorV2-процессе (масштаб Windows 125% и выше)
				// берёт только долю картинки 96/dpi и растягивает её на лист:
				// на бумаге остаётся увеличенная шапка. StretchBlt пишет пиксели как есть.
				DrawPage(e.Graphics!, image, _drawable, _unitsPerHundredth);

				_pageIndex++;
				e.HasMorePages = _pageIndex < _pages.Count && (_pageIndex + 1) <= _toPage;
			}
		}

		private static void DrawPage(Graphics graphics, Image image, RectangleF drawable, float unitsPerHundredth)
		{
			var bitmap = image as Bitmap;
			if (bitmap == null || unitsPerHundredth <= 0)
			{
				graphics.DrawImage(image, drawable);
				return;
			}

			int x, y, width, height;
			PrintDeviceMap.ToDevicePixels(
				drawable.X,
				drawable.Y,
				drawable.Width,
				drawable.Height,
				unitsPerHundredth,
				graphics.DpiX,
				graphics.DpiY,
				out x,
				out y,
				out width,
				out height);
			if (width < 1 || height < 1)
				return;

			var dest = graphics.GetHdc();
			var hBitmap = IntPtr.Zero;
			var memory = IntPtr.Zero;
			var previous = IntPtr.Zero;
			try
			{
				hBitmap = bitmap.GetHbitmap();
				memory = CreateCompatibleDC(dest);
				previous = SelectObject(memory, hBitmap);
				SetStretchBltMode(dest, 4);
				SetBrushOrgEx(dest, 0, 0, IntPtr.Zero);
				StretchBlt(dest, x, y, width, height, memory, 0, 0, bitmap.Width, bitmap.Height, 0x00CC0020);
			}
			finally
			{
				if (memory != IntPtr.Zero)
				{
					SelectObject(memory, previous);
					DeleteDC(memory);
				}

				if (hBitmap != IntPtr.Zero)
					DeleteObject(hBitmap);
				graphics.ReleaseHdc(dest);
			}
		}

		[DllImport("gdi32.dll")]
		private static extern bool StretchBlt(
			IntPtr dest, int x, int y, int width, int height,
			IntPtr source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int rop);

		[DllImport("gdi32.dll")]
		private static extern int SetStretchBltMode(IntPtr hdc, int mode);

		[DllImport("gdi32.dll")]
		private static extern bool SetBrushOrgEx(IntPtr hdc, int x, int y, IntPtr previous);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

		[DllImport("gdi32.dll")]
		private static extern bool DeleteDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr obj);
	}
}
