using CellReport.Avalonia.Services;
using CellReport.Core;
using CellReport.Presenters;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace CellReport.Avalonia
{
	public class AvaloniaPrintPresenter : BasePrintPresenter
	{
		private const float A4WidthMm = 210f;
		private const float A4HeightMm = 297f;

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
			_pageImages.Clear();
			BeforePrinting();

			var bounds = GetPrintableBounds();
			var pageSize = GetPagePixelSize();
			var hasMore = true;

			while (hasMore)
			{
				var morePages = false;
				var pageData = Renderer.CreateImage(pageSize.Width, pageSize.Height, context =>
				{
					if (context is SKCanvas canvas)
						canvas.Clear(SKColors.White);
					morePages = RenderNextPage(context, bounds);
				});

				if (pageData.Length > 0)
					_pageImages.Add(pageData);

				hasMore = morePages;
			}
		}

		private bool RenderNextPage(object context, Rectangle bounds)
		{
			BeginRender();
			SetExpressionPrintMode(true);
			try
			{
				if (CountOfPrintedPages == 0)
				{
					RecalcScale(bounds);
					CalcCountOfPages = CalcPages(bounds);
					BeforePrinting();
					RecalcScale(bounds);
				}

				var oldTop = TopRow;
				var oldLeft = LeftColumn;
				CalcPageEndCheckHasMorePage(bounds, out var rightBottom);
				TopRow = oldTop;
				LeftColumn = oldLeft;
				ExecuteExpressions(new CellLocation { Row = TopRow, Column = LeftColumn }, rightBottom);

				return RenderPageAndCheckHasMorePage(context, bounds);
			}
			finally
			{
				SetExpressionPrintMode(false);
				EndRender();
			}
		}

		internal Rectangle GetPrintableBounds()
		{
			GetPageSizeMm(out var pageWidthMm, out var pageHeightMm);
			var margins = Manager.PrintSettings.Margins;

			var left = UnitsConverter.ConvertValue(margins.Left, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiX);
			var top = UnitsConverter.ConvertValue(margins.Top, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiY);
			var right = UnitsConverter.ConvertValue(margins.Right, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiX);
			var bottom = UnitsConverter.ConvertValue(margins.Bottom, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiY);
			var pageWidth = UnitsConverter.ConvertValue(pageWidthMm, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiX);
			var pageHeight = UnitsConverter.ConvertValue(pageHeightMm, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiY);

			return new Rectangle(left, top, pageWidth - left - right, pageHeight - top - bottom);
		}

		internal (int Width, int Height) GetPagePixelSize()
		{
			GetPageSizeMm(out var pageWidthMm, out var pageHeightMm);
			var width = (int)Math.Ceiling(UnitsConverter.ConvertValue(pageWidthMm, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiX));
			var height = (int)Math.Ceiling(UnitsConverter.ConvertValue(pageHeightMm, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiY));
			return (width, height);
		}

		private void GetPageSizeMm(out float pageWidthMm, out float pageHeightMm)
		{
			if (Manager.PrintSettings.Landscape)
			{
				pageWidthMm = A4HeightMm;
				pageHeightMm = A4WidthMm;
			}
			else
			{
				pageWidthMm = A4WidthMm;
				pageHeightMm = A4HeightMm;
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
			// Dialog is shown from ReportPrintService / ViewModel.
		}

		public override void ShowPreviewDialog()
		{
			// Preview window is shown from ReportPrintService / ViewModel.
		}

		protected override Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			return AvaloniaRenderer.GetTextSize(rect, text, fontFamily, fontSize, fontStyle);
		}
	}
}
