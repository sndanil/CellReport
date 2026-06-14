using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CellReport.Avalonia.Services
{
	internal static class ReportPrintPlatform
	{
		public static void PrintPages(CellManager manager, IReadOnlyList<byte[]> pages, bool useCustom, string? printerName, int fromPage = 1, int toPage = int.MaxValue)
		{
			if (pages == null || pages.Count == 0)
				return;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				WindowsPrintHelper.Print(manager, pages, useCustom, printerName, fromPage, toPage);
				return;
			}

			throw new PlatformNotSupportedException(ReportLocalization.Get("Report/Print/NotSupported"));
		}

		public static bool IsPrintingSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
	}
}
