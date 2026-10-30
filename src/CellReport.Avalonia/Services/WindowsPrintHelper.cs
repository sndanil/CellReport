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
			private float _pageDpi = 96f;
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

				// Ширина рулона (и любой другой бумаги) должна браться у принтера,
				// иначе страница A4 растягивается/сжимается драйвером до ленты.
				var pageSize = GetPageSize(document);
				_pageDpi = pageSize.Dpi;
				_presenter.BuildPages(pageSize.WidthMm, pageSize.HeightMm, pageSize.Dpi);
				_pages = _presenter.PageImages;
				if (_pages.Count == 0)
					return;

				_fromPage = Math.Min(Math.Max(1, _requestedFromPage), _pages.Count);
				_toPage = Math.Min(_pages.Count, Math.Max(_requestedToPage, _fromPage));

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

			private static PrinterPageSize GetPageSize(PrintDocument document)
			{
				try
				{
					var settings = document.DefaultPageSettings;
					var paper = settings.PaperSize;
					if (paper != null && paper.Width > 0 && paper.Height > 0)
					{
						var widthMm = paper.Width * 25.4f / 100f;
						var heightMm = paper.Height * 25.4f / 100f;
						var dpi = WindowsPrinterService.GetDpi(settings);
						return settings.Landscape
							? new PrinterPageSize(heightMm, widthMm, dpi)
							: new PrinterPageSize(widthMm, heightMm, dpi);
					}

					return WindowsPrinterService.GetPageSize(document.PrinterSettings.PrinterName, settings.Landscape);
				}
				catch
				{
					return WindowsPrinterService.GetPageSize(null, false);
				}
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

				// Страница строится в разрешении принтера. Пиксельные координаты
				// позволяют на термопринтере отрисовать её 1:1, без повторной
				// интерполяции, которая даёт «грязные» полутона.
				var dpiX = e.Graphics!.DpiX;
				var dpiY = e.Graphics.DpiY;
				if (dpiX <= 0)
					dpiX = _pageDpi;
				if (dpiY <= 0)
					dpiY = _pageDpi;

				var scaleX = dpiX / _pageDpi;
				var scaleY = dpiY / _pageDpi;
				var destination = new RectangleF(
					-e.PageSettings.HardMarginX * dpiX / 100f,
					-e.PageSettings.HardMarginY * dpiY / 100f,
					image.Width * scaleX,
					image.Height * scaleY);

				e.Graphics.PageUnit = System.Drawing.GraphicsUnit.Pixel;
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
