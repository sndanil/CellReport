using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;

namespace CellReport.Avalonia.Services
{
	internal readonly struct PrinterPageSize
	{
		public PrinterPageSize(float widthMm, float heightMm)
		{
			WidthMm = widthMm;
			HeightMm = heightMm;
		}

		public float WidthMm { get; }

		public float HeightMm { get; }
	}

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

		/// <summary>
		/// Возвращает размер бумаги выбранного принтера в миллиметрах.
		/// Для рулонных принтеров это фактическая ширина ленты, а не A4.
		/// </summary>
		public static PrinterPageSize GetPageSize(string? printerName, bool landscape)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				try
				{
					var settings = new PrinterSettings();
					if (!string.IsNullOrWhiteSpace(printerName)
						&& PrinterSettings.InstalledPrinters.Cast<string>().Any(item => item == printerName))
					{
						settings.PrinterName = printerName;
					}

					if (settings.IsValid)
					{
						var paper = settings.DefaultPageSettings.PaperSize;
						if (paper != null && paper.Width > 0 && paper.Height > 0)
						{
							var widthMm = paper.Width * 25.4f / 100f;
							var heightMm = paper.Height * 25.4f / 100f;
							return landscape
								? new PrinterPageSize(heightMm, widthMm)
								: new PrinterPageSize(widthMm, heightMm);
						}
					}
				}
				catch
				{
					// ниже используем A4
				}
			}

			return landscape
				? new PrinterPageSize(297f, 210f)
				: new PrinterPageSize(210f, 297f);
		}
	}
}
