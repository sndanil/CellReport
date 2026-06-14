using CellReport.Core;
using CellReport.Presenters;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace CellReport.Export.Pdf
{
	public sealed class PrintPageImageBuilder : BasePrintPresenter
	{
		private const float A4WidthMm = 210f;
		private const float A4HeightMm = 297f;
		private const float PdfDpi = 72f;
		private const float RenderDpi = 96f;
		private const string PageBackground = "#FFFFFFFF";

		private readonly Func<Rectangle, string, string, float, FontStyle, Size> _getTextSize;
		private readonly List<byte[]> _pages = new List<byte[]>();

		public PrintPageImageBuilder(
			CellManager manager,
			BaseRenderer renderer,
			Func<Rectangle, string, string, float, FontStyle, Size> getTextSize = null)
				: base(manager)
		{
			if (renderer == null)
				throw new ArgumentNullException(nameof(renderer));

			Renderer = renderer;
			_getTextSize = getTextSize;
			DeviceDpiX = RenderDpi;
			DeviceDpiY = RenderDpi;
			CurrentUnit = GraphicsUnit.Pixel;
		}

		public IReadOnlyList<byte[]> Pages
		{
			get { return _pages; }
		}

		public int PageWidthPx { get; private set; }

		public int PageHeightPx { get; private set; }

		public void ExportVectorPdf(Stream stream)
		{
			BeforePrinting();

			var bounds = GetPrintableBounds();
			var pageSize = GetPagePixelSize();
			PageWidthPx = pageSize.Width;
			PageHeightPx = pageSize.Height;

			var widthPt = pageSize.Width * PdfDpi / RenderDpi;
			var heightPt = pageSize.Height * PdfDpi / RenderDpi;
			var pdfScale = PdfDpi / RenderDpi;

			using (var document = SKDocument.CreatePdf(stream))
			{
				var hasMore = true;
				while (hasMore)
				{
					using (var canvas = document.BeginPage(widthPt, heightPt))
					{
						canvas.Scale(pdfScale, pdfScale);
						Renderer.RenderFillRect(canvas,
							new Rectangle(0, 0, pageSize.Width, pageSize.Height),
							0,
							PageBackground);
						hasMore = RenderNextPage(canvas, bounds);
					}

					document.EndPage();
				}

				document.Close();
			}
		}

		public void BuildPages()
		{
			_pages.Clear();
			BeforePrinting();

			var bounds = GetPrintableBounds();
			var pageSize = GetPagePixelSize();
			PageWidthPx = pageSize.Width;
			PageHeightPx = pageSize.Height;

			var hasMore = true;
			while (hasMore)
			{
				var morePages = false;
				var pageData = Renderer.CreateImage(pageSize.Width, pageSize.Height, context =>
				{
					Renderer.RenderFillRect(context,
						new Rectangle(0, 0, pageSize.Width, pageSize.Height),
						0,
						PageBackground);
					morePages = RenderNextPage(context, bounds);
				});

				if (pageData.Length > 0)
					_pages.Add(pageData);

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
				CellLocation rightBottom;
				CalcPageEndCheckHasMorePage(bounds, out rightBottom);
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
			float pageWidthMm;
			float pageHeightMm;
			GetPageSizeMm(out pageWidthMm, out pageHeightMm);
			var margins = Manager.PrintSettings.Margins;

			var left = UnitsConverter.ConvertValue(margins.Left, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiX);
			var top = UnitsConverter.ConvertValue(margins.Top, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiY);
			var right = UnitsConverter.ConvertValue(margins.Right, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiX);
			var bottom = UnitsConverter.ConvertValue(margins.Bottom, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiY);
			var pageWidth = UnitsConverter.ConvertValue(pageWidthMm, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiX);
			var pageHeight = UnitsConverter.ConvertValue(pageHeightMm, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiY);

			return new Rectangle(left, top, pageWidth - left - right, pageHeight - top - bottom);
		}

		internal PagePixelSize GetPagePixelSize()
		{
			float pageWidthMm;
			float pageHeightMm;
			GetPageSizeMm(out pageWidthMm, out pageHeightMm);
			var width = (int)Math.Ceiling(UnitsConverter.ConvertValue(pageWidthMm, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiX));
			var height = (int)Math.Ceiling(UnitsConverter.ConvertValue(pageHeightMm, GraphicsUnit.Millimeter, CurrentUnit, DeviceDpiY));
			return new PagePixelSize(width, height);
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

		protected override Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			return _getTextSize != null
				? _getTextSize(rect, text, fontFamily, fontSize, fontStyle)
				: Size.Empty;
		}

		protected override void OnPrint(bool useCustom, string printerName = null)
		{
		}

		public override void ShowCustomPage()
		{
		}

		public override void ShowPreviewDialog()
		{
		}

		internal struct PagePixelSize
		{
			public PagePixelSize(int width, int height)
			{
				Width = width;
				Height = height;
			}

			public int Width { get; }

			public int Height { get; }
		}
	}
}
