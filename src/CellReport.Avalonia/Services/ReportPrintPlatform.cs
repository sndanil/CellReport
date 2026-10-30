using System;
using System.Runtime.InteropServices;

namespace CellReport.Avalonia.Services
{
	internal static class ReportPrintPlatform
	{
		public static void Print(AvaloniaPrintPresenter presenter, bool useCustom, string? printerName, int fromPage = 1, int toPage = int.MaxValue)
		{
			if (presenter == null)
				return;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				WindowsPrintHelper.Print(presenter, useCustom, printerName, fromPage, toPage);
				return;
			}

			throw new PlatformNotSupportedException(ReportLocalization.Get("Report/Print/NotSupported"));
		}

		public static bool IsPrintingSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
	}
}
