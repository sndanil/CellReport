using CellReport.Avalonia;
using CellReport.Core;
using CellReport.Presenters;
using CellReport.Presenters.WinForms;
using System;
using System.Drawing.Printing;
using System.Linq;

namespace CellReport.Avalonia.Services
{
	internal static class GdiPrintHelper
	{
		public static void Print(AvaloniaPrintPresenter presenter, bool useCustom, string? printerName, int fromPage, int toPage)
		{
			if (presenter == null)
				return;

			using (DesignModeScope.Suspend(presenter.Manager))
			{
				var session = new GdiPrintSession(presenter.Manager);
				session.Print(useCustom, printerName, fromPage, toPage);
			}
		}

		private sealed class GdiPrintSession : BasePrintPresenter
		{
			private float _hardMarginX;
			private float _hardMarginY;

			public GdiPrintSession(CellManager manager)
				: base(manager)
			{
				Renderer = new GdiPlusRenderer();
				CurrentUnit = GraphicsUnit.Display;
			}

			public void Print(bool useCustom, string? printerName, int fromPage, int toPage)
			{
				using (var document = CreateDocument())
				{
					if (!string.IsNullOrEmpty(printerName) &&
					    PrinterSettings.InstalledPrinters.Cast<string>().Any(name => name == printerName))
					{
						document.PrinterSettings.PrinterName = printerName;
					}

					if (useCustom && toPage != int.MaxValue)
					{
						document.PrinterSettings.MinimumPage = 1;
						document.PrinterSettings.MaximumPage = Math.Max(1, toPage);
						document.PrinterSettings.PrintRange = PrintRange.SomePages;
						document.PrinterSettings.FromPage = Math.Max(1, fromPage);
						document.PrinterSettings.ToPage = Math.Max(document.PrinterSettings.FromPage, toPage);
					}

					document.Print();
				}
			}

			private PrintDocument CreateDocument()
			{
				var reportMargins = Manager.PrintSettings.Margins ?? new CellReport.Core.Margins(0);
				var document = new PrintDocument
				{
					DocumentName = Manager.Properties.DocumentName,
					DefaultPageSettings =
					{
						Landscape = Manager.PrintSettings.Landscape,
						Margins = new System.Drawing.Printing.Margins(
							MillimetersToHundredths(reportMargins.Left),
							MillimetersToHundredths(reportMargins.Right),
							MillimetersToHundredths(reportMargins.Top),
							MillimetersToHundredths(reportMargins.Bottom))
					}
				};
				document.PrinterSettings.Copies = Manager.PrintSettings.Copies;
				document.PrintPage += OnPrintPage;
				return document;
			}

			private static int MillimetersToHundredths(float millimeters)
			{
				return Convert.ToInt32(UnitsConverter.ConvertValue(millimeters, GraphicsUnit.Millimeter, GraphicsUnit.Display, 0));
			}

			private void OnPrintPage(object sender, PrintPageEventArgs e)
			{
				BeginRender();
				e.HasMorePages = true;
				var document = sender as PrintDocument;
				if (document == null)
					throw new ArgumentException("sender");

				SetExpressionPrintMode(true);
				try
				{
					DeviceDpiX = e.Graphics!.DpiX;
					DeviceDpiY = e.Graphics.DpiY;
					var bounds = e.MarginBounds.ToCellRecttangle();
					if (CountOfPrintedPages == 0)
					{
						_hardMarginX = e.PageSettings.HardMarginX;
						_hardMarginY = e.PageSettings.HardMarginY;
					}

					var printableArea = e.PageSettings.PrintableArea;
					var realPrintableArea = new System.Drawing.RectangleF(
						e.PageSettings.Landscape ? printableArea.Y : printableArea.X,
						e.PageSettings.Landscape ? printableArea.X : printableArea.Y,
						e.PageSettings.Landscape ? printableArea.Height : printableArea.Width,
						e.PageSettings.Landscape ? printableArea.Width : printableArea.Height);

					bounds.X = Math.Max(0, e.PageSettings.Margins.Left - _hardMarginX);
					if (bounds.X > 0)
						bounds.X++;
					bounds.Y = Math.Max(0, e.PageSettings.Margins.Top - _hardMarginY);
					if (bounds.Y > 0)
						bounds.Y++;

					if (e.PageSettings.Margins.Left < _hardMarginX)
						bounds.Width -= _hardMarginX - e.PageSettings.Margins.Left;
					if (bounds.Width > realPrintableArea.Width)
						bounds.Width -= bounds.Width - realPrintableArea.Width;

					if (e.PageSettings.Margins.Top < _hardMarginY)
						bounds.Height -= _hardMarginY - e.PageSettings.Margins.Top;
					if (bounds.Height > realPrintableArea.Height)
						bounds.Height -= bounds.Height - realPrintableArea.Height;

					if (CountOfPrintedPages == 0)
					{
						RecalcScale(bounds);
						CalcCountOfPages = CalcPages(bounds);
						document.DocumentName = Manager.Properties.DocumentName + " (" + CalcCountOfPages + ")";
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

					if (document.PrinterSettings.PrintRange == PrintRange.SomePages)
					{
						if (CountOfPrintedPages < document.PrinterSettings.FromPage)
						{
							do
							{
								e.HasMorePages = CalcPageEndCheckHasMorePage(bounds, out rightBottom);
								ExecuteExpressions(new CellLocation { Row = TopRow, Column = LeftColumn }, rightBottom);
							}
							while (CountOfPrintedPages < document.PrinterSettings.FromPage);
						}

						if (CountOfPrintedPages >= document.PrinterSettings.ToPage)
							e.HasMorePages = false;
					}

					e.HasMorePages = RenderPageAndCheckHasMorePage(e.Graphics, bounds) && e.HasMorePages;
				}
				finally
				{
					SetExpressionPrintMode(false);
					EndRender();
				}
			}

			protected override Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
			{
				return GdiPlusRenderer.GetTextSize(rect, text, fontFamily, fontSize, fontStyle);
			}

			protected override void OnPrint(bool useCustom, string printerName = null)
			{
				Print(useCustom, printerName, 1, int.MaxValue);
			}

			public override void ShowCustomPage()
			{
			}

			public override void ShowPreviewDialog()
			{
			}
		}
	}
}
