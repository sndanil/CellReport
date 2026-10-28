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
		public static void Print(CellManager manager, IReadOnlyList<byte[]> pages, bool useCustom, string? printerName, int fromPage = 1, int toPage = int.MaxValue)
		{
			if (pages.Count == 0)
				return;

			new PrintOperation(manager, pages, fromPage, toPage).Run(useCustom, printerName);
		}

		private sealed class PrintOperation
		{
			private readonly IList<byte[]> _pages;
			private readonly CellManager _manager;
			private readonly int _fromPage;
			private readonly int _toPage;
			private int _pageIndex;

			public PrintOperation(CellManager manager, IReadOnlyList<byte[]> pages, int fromPage, int toPage)
			{
				_manager = manager;
				_pages = pages.ToList();
				_fromPage = Math.Max(1, fromPage);
				_toPage = Math.Min(pages.Count, toPage <= 0 ? pages.Count : toPage);
			}

			public void Run(bool useCustom, string? printerName)
			{
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

			private PrintDocument CreateDocument()
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

			private void OnPrintPage(object sender, PrintPageEventArgs e)
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
}
