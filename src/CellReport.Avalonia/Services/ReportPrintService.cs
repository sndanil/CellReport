using CellReport.Avalonia.Services;
using CellReport.Avalonia.Views;
using CellReport.Core;
using Avalonia;
using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace CellReport.Avalonia
{
	public static class ReportPrintService
	{
		public static async Task ShowPreviewAsync(Visual host, CellManager manager, string? printerName = null)
		{
			var window = TopLevel.GetTopLevel(host) as Window;
			if (window == null)
				return;

			var presenter = new AvaloniaPrintPresenter(manager);
			// Лист целиком, поля отчёта внутри. Иначе белая страница — это уже
			// область без полей, и форма выглядит крупнее, чем на бумаге.
			var pageSize = PrinterService.GetPageSize(printerName, manager.PrintSettings.Landscape);
			var margins = manager.PrintSettings.Margins ?? new Margins(0);
			presenter.BuildPages(pageSize.WidthMm, pageSize.HeightMm, pageSize.Dpi, margins);

			var preview = new PrintPreviewWindow(presenter, printerName);
			await preview.ShowDialog(window);
		}

		public static async Task<bool> ShowPageSetupAsync(Visual host, CellManager manager)
		{
			var window = TopLevel.GetTopLevel(host) as Window;
			if (window == null)
				return false;

			var dialog = new PageSetupDialog(manager.PrintSettings);
			var result = await dialog.ShowDialog<bool?>(window);
			return result == true;
		}

		public static async Task PrintAsync(Visual host, CellManager manager, bool custom, string? printerName = null)
		{
			try
			{
				var presenter = new AvaloniaPrintPresenter(manager);
				if (custom)
				{
					var pageSize = PrinterService.GetContentPageSize(printerName, manager);
					// Для диалога нужен только счётчик страниц; печать затем
					// перестраивается выбранным способом.
					presenter.BuildPages(pageSize.WidthMm, pageSize.HeightMm, pageSize.Dpi, new Margins(0));
					var window = TopLevel.GetTopLevel(host) as Window;
					if (window == null)
						return;

					var options = new PrintOptionsDialog(presenter.PageCount, manager.PrintSettings, printerName);
					var result = await options.ShowDialog<bool?>(window);
					if (result != true)
						return;

					presenter.PrintPages(options.FromPage, options.ToPage, options.PrinterName, (short)options.Copies);
				}
				else
				{
					presenter.Print(printerName);
				}
			}
			catch (PlatformNotSupportedException)
			{
				await ReportExportService.ShowMessageAsync(host, ReportLocalization.Get("Report/Print/NotSupported"));
			}
			catch (Exception ex)
			{
				await ReportExportService.ShowMessageAsync(host, ex.Message);
			}
		}
	}
}
