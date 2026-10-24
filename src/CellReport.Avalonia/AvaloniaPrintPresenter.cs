using CellReport.Avalonia.Services;
using CellReport.Core;
using CellReport.Export.Pdf;
using CellReport.Presenters;
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
			using (DesignModeScope.Suspend())
			{
				_pageImages.Clear();

				var builder = new PrintPageImageBuilder(Manager, Renderer, AvaloniaRenderer.GetTextSize);
				builder.BuildPages();
				_pageImages.AddRange(builder.Pages);
			}
		}

		protected override void OnPrint(bool useCustom, string? printerName = null)
		{
			BuildPages();
			ReportPrintPlatform.PrintPages(Manager, _pageImages, useCustom, printerName);
		}

		public void PrintPages(int fromPage, int toPage, string? printerName, short copies)
		{
			Manager.PrintSettings.Copies = copies;
			ReportPrintPlatform.PrintPages(Manager, _pageImages, useCustom: true, printerName, fromPage, toPage);
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
