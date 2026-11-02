using CellReport.Avalonia;
using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;

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

			private static int MillimetersToHundredthsInch(float millimeters)
			{
				return (int)Math.Round(millimeters * 100f / 25.4f);
			}

			private void EnsurePages(PrintPageEventArgs e)
			{
				if (_pages.Count > 0)
					return;

				// MarginBounds уже учитывает поля из DefaultPageSettings и
				// фактическую печатную область конкретного драйвера/бумаги.
				var bounds = e.MarginBounds;
				var widthMm = Math.Max(1f, bounds.Width * 25.4f / 100f);
				var heightMm = Math.Max(1f, bounds.Height * 25.4f / 100f);
				var dpi = Math.Max(e.Graphics!.DpiX, e.Graphics.DpiY);
				if (dpi <= 0)
					dpi = 96f;

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

				// Страница построена ровно под фактическую печатную область.
				// Начало координат Graphics у PrintDocument находится в
				// печатной области (hard margin), а MarginBounds — от края
				// страницы, поэтому компенсируем hard margin.
				var destination = new RectangleF(
					e.MarginBounds.X - e.PageSettings.HardMarginX,
					e.MarginBounds.Y - e.PageSettings.HardMarginY,
					e.MarginBounds.Width,
					e.MarginBounds.Height);

				e.Graphics!.PageUnit = System.Drawing.GraphicsUnit.Display;
				e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
				e.Graphics.DrawImage(image, destination);

				_pageIndex++;
				e.HasMorePages = _pageIndex < _pages.Count && (_pageIndex + 1) <= _toPage;
			}
		}
	}
}
