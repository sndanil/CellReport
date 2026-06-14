using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;

namespace CellReport.Avalonia.Services
{
	internal static class WindowsPrintHelper
	{
		private static IList<byte[]> _pages = Array.Empty<byte[]>();
		private static int _pageIndex;
		private static CellManager _manager = null!;
		private static int _fromPage = 1;
		private static int _toPage = int.MaxValue;

		public static void Print(CellManager manager, IReadOnlyList<byte[]> pages, bool useCustom, string? printerName, int fromPage = 1, int toPage = int.MaxValue)
		{
			if (pages.Count == 0)
				return;

			_manager = manager;
			_pages = pages.ToList();
			_pageIndex = 0;
			_fromPage = Math.Max(1, fromPage);
			_toPage = Math.Min(pages.Count, toPage <= 0 ? pages.Count : toPage);

			using var document = CreateDocument();
			if (!string.IsNullOrEmpty(printerName) &&
			    PrinterSettings.InstalledPrinters.Cast<string>().Any(p => p == printerName))
			{
				document.PrinterSettings.PrinterName = printerName;
			}

			if (useCustom)
			{
				document.PrinterSettings.PrintRange = PrintRange.SomePages;
				document.PrinterSettings.FromPage = _fromPage;
				document.PrinterSettings.ToPage = _toPage;
			}

			document.Print();
		}

		private static PrintDocument CreateDocument()
		{
			var document = new PrintDocument
			{
				DocumentName = _manager.Properties.DocumentName,
				DefaultPageSettings =
				{
					Landscape = _manager.PrintSettings.Landscape,
					Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0)
				}
			};
			document.PrinterSettings.Copies = _manager.PrintSettings.Copies;
			document.PrintPage += OnPrintPage;
			return document;
		}

		private static void OnPrintPage(object sender, PrintPageEventArgs e)
		{
			var currentPageNumber = _pageIndex + 1;

			if (currentPageNumber < _fromPage)
			{
				_pageIndex++;
				e.HasMorePages = _pageIndex < _pages.Count;
				return;
			}

			if (currentPageNumber > _toPage)
			{
				e.HasMorePages = false;
				return;
			}

			using var stream = new MemoryStream(_pages[_pageIndex]);
			using var image = Image.FromStream(stream);
			e.Graphics!.DrawImage(image, e.MarginBounds);

			_pageIndex++;
			e.HasMorePages = _pageIndex < _pages.Count && (_pageIndex + 1) <= _toPage;
		}
	}
}
