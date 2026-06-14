using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;

namespace CellReport.Avalonia.Services
{
	internal static class WindowsPrinterService
	{
		public static IReadOnlyList<string> GetInstalledPrinters()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return Array.Empty<string>();

			try
			{
				return PrinterSettings.InstalledPrinters.Cast<string>().ToList();
			}
			catch
			{
				return Array.Empty<string>();
			}
		}

		public static string? GetDefaultPrinter()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return null;

			try
			{
				var settings = new PrinterSettings();
				return string.IsNullOrWhiteSpace(settings.PrinterName) ? null : settings.PrinterName;
			}
			catch
			{
				return null;
			}
		}
	}
}
