using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;

namespace CellReport.Avalonia.Services
{
	internal readonly struct PrinterPageSize
	{
		public PrinterPageSize(float widthMm, float heightMm, float dpi, string? paperName = null)
		{
			WidthMm = widthMm;
			HeightMm = heightMm;
			Dpi = dpi;
			PaperName = paperName;
		}

		public float WidthMm { get; }

		public float HeightMm { get; }

		public float Dpi { get; }

		public string? PaperName { get; }
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
						var page = settings.DefaultPageSettings;
						var paper = page.PaperSize;
						if (paper != null && paper.Width > 0 && paper.Height > 0)
						{
							var widthMm = paper.Width * 25.4f / 100f;
							var heightMm = paper.Height * 25.4f / 100f;
							var dpi = GetDpi(page);
							var name = string.IsNullOrWhiteSpace(paper.PaperName) ? null : paper.PaperName;
							return landscape
								? new PrinterPageSize(heightMm, widthMm, dpi, name)
								: new PrinterPageSize(widthMm, heightMm, dpi, name);
						}
					}
				}
				catch
				{
					// ниже используем A4
				}
			}

			return landscape
				? new PrinterPageSize(297f, 210f, 96f, "A4")
				: new PrinterPageSize(210f, 297f, 96f, "A4");
		}

		/// <summary>
		/// Размер печатной области: бумага минус поля отчёта. Именно этот размер
		/// используется при построении страниц для печати (MarginBounds).
		/// </summary>
		public static PrinterPageSize GetContentPageSize(string? printerName, CellManager manager)
		{
			var page = GetPageSize(printerName, manager.PrintSettings.Landscape);
			var margins = manager.PrintSettings.Margins ?? new CellReport.Core.Margins(0);
			return new PrinterPageSize(
				Math.Max(1f, page.WidthMm - margins.Left - margins.Right),
				Math.Max(1f, page.HeightMm - margins.Top - margins.Bottom),
				page.Dpi,
				page.PaperName);
		}

		public static float GetDpi(PageSettings page)
		{
			try
			{
				var resolution = page.PrinterResolution;
				var dpi = Math.Max(resolution.X, resolution.Y);
				if (dpi <= 0)
					return 96f;

				// Ограничение защищает от гигантских растровых страниц на 600–1200 dpi.
				return Math.Min(dpi, 300f);
			}
			catch
			{
				return 96f;
			}
		}
	}
}
