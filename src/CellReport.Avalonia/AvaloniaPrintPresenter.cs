using CellReport.Avalonia.Services;
using CellReport.Core;
using CellReport.Export.Pdf;
using CellReport.Presenters;
using CellReport.Presenters.Skia;
using System;
using System.Collections.Generic;

namespace CellReport.Avalonia
{
	public class AvaloniaPrintPresenter : BasePrintPresenter
	{
		private readonly List<byte[]> _pageImages = new();

		public AvaloniaPrintPresenter(CellManager manager)
				: base(manager)
		{
			Renderer = new AvaloniaRenderer();
			DeviceDpiX = 96;
			DeviceDpiY = 96;
			CurrentUnit = GraphicsUnit.Pixel;
		}

		public IReadOnlyList<byte[]> PageImages => _pageImages;

		public int PageCount => _pageImages.Count;

		public void BuildPages()
		{
			BuildPages(0, 0, 96f);
		}

		public void BuildPages(float pageWidthMm, float pageHeightMm)
		{
			BuildPages(pageWidthMm, pageHeightMm, 96f);
		}

		/// <summary>
		/// Строит страницы под конкретный размер бумаги (в миллиметрах) и
		/// разрешение принтера. Нулевые размеры означают стандартный лист A4.
		/// </summary>
		public void BuildPages(float pageWidthMm, float pageHeightMm, float dpi)
		{
			BuildPages(pageWidthMm, pageHeightMm, dpi, null);
		}

		/// <summary>
		/// Строит страницы под конкретную печатную область. Если <paramref name="margins"/>
		/// заданы, размеры страницы считаются уже без полей; для печати по фактическому
		/// MarginBounds передаются нулевые поля.
		/// </summary>
		public void BuildPages(float pageWidthMm, float pageHeightMm, float dpi, Margins? margins)
		{
			using (DesignModeScope.Suspend(Manager))
			{
				_pageImages.Clear();
				var builder = CreateBuilder(pageWidthMm, pageHeightMm, dpi, margins);
				builder.BuildPages();
				_pageImages.AddRange(builder.Pages);
			}
		}

		public void ExportPdf(System.IO.Stream stream, float pageWidthMm, float pageHeightMm, float dpi, Margins? margins)
		{
			using (DesignModeScope.Suspend(Manager))
				CreateBuilder(pageWidthMm, pageHeightMm, dpi, margins).ExportVectorPdf(stream);
		}

		private PrintPageImageBuilder CreateBuilder(float pageWidthMm, float pageHeightMm, float dpi, Margins? margins)
		{
			if (Renderer is SkiaReportRenderer skiaRenderer)
			{
				skiaRenderer.Dpi = dpi;
				skiaRenderer.TextAntialias = dpi <= 96f;
			}

			Func<Rectangle, string, string, float, FontStyle, Size> getTextSize =
				(rect, text, family, size, style) => AvaloniaRenderer.GetTextSize(rect, text, family, size, style, dpi);

			return new PrintPageImageBuilder(
				Manager,
				Renderer,
				getTextSize,
				pageWidthMm,
				pageHeightMm,
				margins,
				dpi);
		}

		protected override void OnPrint(bool useCustom, string? printerName = null)
		{
			ReportPrintPlatform.Print(this, useCustom, printerName);
		}

		public void PrintPages(int fromPage, int toPage, string? printerName, short copies)
		{
			Manager.PrintSettings.Copies = copies;
			ReportPrintPlatform.Print(this, useCustom: true, printerName, fromPage, toPage);
		}

		public override void ShowCustomPage()
		{
		}

		public override void ShowPreviewDialog()
		{
		}

		protected override Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			return AvaloniaRenderer.GetTextSize(rect, text, fontFamily, fontSize, fontStyle);
		}
	}
}
