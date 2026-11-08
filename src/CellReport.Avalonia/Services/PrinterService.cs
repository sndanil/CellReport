using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CellReport.Avalonia.Services
{
	internal static class PrinterService
	{
		public static bool IsPrintingSupported =>
			RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
			RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

		public static IReadOnlyList<string> GetInstalledPrinters()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return WindowsPrinterService.GetInstalledPrinters();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return CupsPrinterService.GetInstalledPrinters();
			return Array.Empty<string>();
		}

		public static string? GetDefaultPrinter()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return WindowsPrinterService.GetDefaultPrinter();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return CupsPrinterService.GetDefaultPrinter();
			return null;
		}

		public static PrinterPageSize GetPageSize(string? printerName, bool landscape)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
			    !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return CupsPrinterService.GetPageSize(printerName, landscape);

			return WindowsPrinterService.GetPageSize(printerName, landscape);
		}

		public static PrinterPageSize GetContentPageSize(string? printerName, CellManager manager)
		{
			var page = GetPageSize(printerName, manager.PrintSettings.Landscape);
			var margins = manager.PrintSettings.Margins ?? new Margins(0);
			return new PrinterPageSize(
				Math.Max(1f, page.WidthMm - margins.Left - margins.Right),
				Math.Max(1f, page.HeightMm - margins.Top - margins.Bottom),
				page.Dpi,
				page.PaperName);
		}
	}
}
