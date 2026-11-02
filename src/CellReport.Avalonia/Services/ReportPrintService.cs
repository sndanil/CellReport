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
			var pageSize = WindowsPrinterService.GetContentPageSize(printerName, manager);
			// Предпросмотр строим по той же печатной области, что и печать:
			// бумага минус поля отчёта, без повторного применения полей.
			presenter.BuildPages(pageSize.WidthMm, pageSize.HeightMm, 96f, new Margins(0));

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
					var pageSize = WindowsPrinterService.GetContentPageSize(printerName, manager);
					// Для диалога нужен только счётчик страниц; печать затем
					// перестраивается в разрешении выбранного принтера.
					presenter.BuildPages(pageSize.WidthMm, pageSize.HeightMm, 96f, new Margins(0));
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
