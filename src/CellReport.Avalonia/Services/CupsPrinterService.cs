using CellReport.Avalonia;
using CellReport.Core;
using CellReport.Presenters.Skia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CellReport.Avalonia.Services
{
	internal static class CupsPrinterService
	{
		private const int TimeoutMs = 8000;
		private const float PdfDpi = 96f;

		public static IReadOnlyList<string> GetInstalledPrinters()
		{
			try
			{
				var output = Run("lpstat", new[] { "-p" }, expectSuccess: false);
				return CupsPrinterQuery.ParsePrinters(output.StdOut);
			}
			catch (InvalidOperationException)
			{
				return Array.Empty<string>();
			}
		}

		public static string? GetDefaultPrinter()
		{
			try
			{
				var output = Run("lpstat", new[] { "-d" }, expectSuccess: false);
				return CupsPrinterQuery.ParseDefaultPrinter(output.StdOut);
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		public static PrinterPageSize GetPageSize(string? printerName, bool landscape)
		{
			var media = ReadDefaultMedia(printerName);
			float width;
			float height;
			if (!CupsPrinterQuery.TryGetPaperMillimetres(media, out width, out height))
			{
				width = 210f;
				height = 297f;
				if (string.IsNullOrEmpty(media))
					media = "A4";
			}

			return landscape
				? new PrinterPageSize(height, width, PdfDpi, media)
				: new PrinterPageSize(width, height, PdfDpi, media);
		}

		public static void Print(AvaloniaPrintPresenter presenter, string? printerName, int fromPage, int toPage)
		{
			var printer = printerName;
			if (string.IsNullOrWhiteSpace(printer))
				printer = GetDefaultPrinter();
			if (string.IsNullOrWhiteSpace(printer))
				throw new InvalidOperationException(ReportLocalization.Get("Report/Print/CupsMissing"));

			var page = GetPageSize(printer, presenter.Manager.PrintSettings.Landscape);
			var margins = presenter.Manager.PrintSettings.Margins ?? new Margins(0);
			var pdfPath = Path.Combine(Path.GetTempPath(), "cellreport-" + Guid.NewGuid().ToString("N") + ".pdf");
			try
			{
				using (var stream = File.Create(pdfPath))
					presenter.ExportPdf(stream, page.WidthMm, page.HeightMm, page.Dpi, margins);

				var arguments = new List<string>
				{
					"-d", printer,
					"-n", Math.Max(1, (int)presenter.Manager.PrintSettings.Copies).ToString()
				};
				if (toPage != int.MaxValue)
				{
					arguments.Add("-P");
					arguments.Add(Math.Max(1, fromPage) + "-" + Math.Max(fromPage, toPage));
				}

				if (!string.IsNullOrEmpty(page.PaperName))
				{
					arguments.Add("-o");
					arguments.Add("media=" + page.PaperName);
				}

				arguments.Add("-o");
				arguments.Add("print-scaling=none");
				arguments.Add(pdfPath);

				var result = Run("lp", arguments, expectSuccess: true);
				if (result.ExitCode != 0)
					throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr);
			}
			finally
			{
				try
				{
					if (File.Exists(pdfPath))
						File.Delete(pdfPath);
				}
				catch (IOException)
				{
				}
			}
		}

		private static string? ReadDefaultMedia(string? printerName)
		{
			try
			{
				var arguments = string.IsNullOrWhiteSpace(printerName)
					? new[] { "-l" }
					: new[] { "-p", printerName, "-l" };
				var output = Run("lpoptions", arguments, expectSuccess: false);
				return CupsPrinterQuery.ParseDefaultMedia(output.StdOut);
			}
			catch (InvalidOperationException)
			{
				return null;
			}
		}

		private static ProcessOutput Run(string fileName, IReadOnlyList<string> arguments, bool expectSuccess)
		{
			var start = new ProcessStartInfo
			{
				FileName = fileName,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};
			start.Environment["LC_ALL"] = "C";
			start.Environment["LANG"] = "C";
			start.Environment["LANGUAGE"] = "C";
			foreach (var argument in arguments)
				start.ArgumentList.Add(argument);

			Process? process;
			try
			{
				process = Process.Start(start);
			}
			catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is FileNotFoundException)
			{
				throw new InvalidOperationException(ReportLocalization.Get("Report/Print/CupsMissing"));
			}

			if (process == null)
				throw new InvalidOperationException(ReportLocalization.Get("Report/Print/CupsMissing"));

			using (process)
			{
				var stdout = process.StandardOutput.ReadToEndAsync();
				var stderr = process.StandardError.ReadToEndAsync();
				if (!process.WaitForExit(TimeoutMs))
				{
					try
					{
						process.Kill(entireProcessTree: true);
					}
					catch (InvalidOperationException)
					{
					}

					throw new InvalidOperationException(ReportLocalization.Get("Report/Print/CupsMissing"));
				}

				var result = new ProcessOutput(process.ExitCode, stdout.GetAwaiter().GetResult(), stderr.GetAwaiter().GetResult());
				if (expectSuccess && result.ExitCode != 0)
					throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr);
				return result;
			}
		}

		private readonly struct ProcessOutput
		{
			public ProcessOutput(int exitCode, string stdOut, string stdErr)
			{
				ExitCode = exitCode;
				StdOut = stdOut ?? string.Empty;
				StdErr = stdErr ?? string.Empty;
			}

			public int ExitCode { get; }

			public string StdOut { get; }

			public string StdErr { get; }
		}
	}
}
