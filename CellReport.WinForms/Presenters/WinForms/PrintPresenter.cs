using System;
using CellReport.Core;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace CellReport.Presenters.WinForms
{
	public class PrintPresenter : BasePrintPresenter
	{
		private PrintAction _action = PrintAction.PrintToPrinter;
		private float _hardMarginX = 0;
		private float _hardMarginY = 0;

		public PrintPresenter(CellManager manager)
				: base(manager)
		{
			Renderer = new GdiPlusRenderer();
			CurrentUnit = GraphicsUnit.Display;
		}

		public PrintDocument GetDocument()
		{
			BeforePrinting();
			PrintDocument doc = new PrintDocument();
			doc.BeginPrint += DocBeginPrint;
			doc.DefaultPageSettings.PrinterSettings.Copies = Manager.PrintSettings.Copies;
			doc.DefaultPageSettings.Landscape = Manager.PrintSettings.Landscape;
			doc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins()
			{
				Left = Convert.ToInt32(UnitsConverter.ConvertValue(Manager.PrintSettings.Margins.Left, GraphicsUnit.Millimeter, GraphicsUnit.Display, 0)),
				Top = Convert.ToInt32(UnitsConverter.ConvertValue(Manager.PrintSettings.Margins.Top, GraphicsUnit.Millimeter, GraphicsUnit.Display, 0)),
				Right = Convert.ToInt32(UnitsConverter.ConvertValue(Manager.PrintSettings.Margins.Right, GraphicsUnit.Millimeter, GraphicsUnit.Display, 0)),
				Bottom = Convert.ToInt32(UnitsConverter.ConvertValue(Manager.PrintSettings.Margins.Bottom, GraphicsUnit.Millimeter, GraphicsUnit.Display, 0))
			};
			doc.DocumentName = Manager.Properties.DocumentName;
			doc.PrintPage += DocPrintPage;

			return doc;
		}

		void DocBeginPrint(object sender, PrintEventArgs e)
		{
			_action = e.PrintAction;
		}

		protected override void OnPrint(bool useCustom)
		{
			PrintDocument doc = GetDocument();
			if (useCustom)
			{
				using (var dialog = new PrintDialog())
				{
					dialog.AllowSomePages = true;
					dialog.UseEXDialog = true;
					dialog.Document = doc;
					if (dialog.ShowDialog() != DialogResult.OK)
						return;
				}
			}
			doc.Print();
		}

		public override void ShowCustomPage()
		{
			using (var dialog = new PageSetupDialog())
			{
				var doc = GetDocument();
				dialog.EnableMetric = true;
				dialog.Document = doc;
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					Manager.PrintSettings.Landscape = doc.DefaultPageSettings.Landscape;
					Manager.PrintSettings.Margins = new Core.Margins(
							UnitsConverter.ConvertValue(doc.DefaultPageSettings.Margins.Left, GraphicsUnit.Display, GraphicsUnit.Millimeter, 0),
							UnitsConverter.ConvertValue(doc.DefaultPageSettings.Margins.Right, GraphicsUnit.Display, GraphicsUnit.Millimeter, 0),
							UnitsConverter.ConvertValue(doc.DefaultPageSettings.Margins.Top, GraphicsUnit.Display, GraphicsUnit.Millimeter, 0),
							UnitsConverter.ConvertValue(doc.DefaultPageSettings.Margins.Bottom, GraphicsUnit.Display, GraphicsUnit.Millimeter, 0)
							);
				}
			}
		}

		public override void ShowPreviewDialog()
		{
			using (PreviewForm previewDialog = new PreviewForm())
			{
				previewDialog.SetPresenter(this);
				previewDialog.ShowDialog();
			}
		}

		private void DocPrintPage(object sender, PrintPageEventArgs e)
		{
			BeginRender();
			e.HasMorePages = true;
			if (!(sender is PrintDocument doc))
				throw new ArgumentException("sender");

			SetExpressionPrintMode(true);
			try
			{
				DeviceDpiX = e.Graphics.DpiX;
				DeviceDpiY = e.Graphics.DpiY;
				var bounds = e.MarginBounds.ToCellRecttangle();
				if (_action == PrintAction.PrintToPrinter)
				{
					if (CountOfPrintedPages == 0)
					{
						_hardMarginX = e.PageSettings.HardMarginX;
						_hardMarginY = e.PageSettings.HardMarginY;
					}
					var printableArea = e.PageSettings.PrintableArea;
					var realPrintableArea = new System.Drawing.RectangleF(
						(e.PageSettings.Landscape ? printableArea.Y : printableArea.X),
						(e.PageSettings.Landscape ? printableArea.X : printableArea.Y),
						(e.PageSettings.Landscape ? printableArea.Height : printableArea.Width),
						(e.PageSettings.Landscape ? printableArea.Width : printableArea.Height)
					);

					bounds.X = Math.Max(0, e.PageSettings.Margins.Left - _hardMarginX);
					if (bounds.X > 0)
						bounds.X++;
					bounds.Y = Math.Max(0, e.PageSettings.Margins.Top - _hardMarginY);
					if (bounds.Y > 0)
						bounds.Y++;

					if (e.PageSettings.Margins.Left < _hardMarginX)
						bounds.Width -= _hardMarginX - e.PageSettings.Margins.Left;
					if (bounds.Width > realPrintableArea.Width)
						bounds.Width -= (bounds.Width - realPrintableArea.Width);

					if (e.PageSettings.Margins.Top < _hardMarginY)
						bounds.Height -= _hardMarginY - e.PageSettings.Margins.Top;
					if (bounds.Height > realPrintableArea.Height)
						bounds.Height -= (bounds.Height - realPrintableArea.Height);
				}

				if (CountOfPrintedPages == 0)
				{
					RecalcScale(bounds);
					CalcCountOfPages = CalcPages(bounds);
					doc.DocumentName = $"{Manager.Properties.DocumentName} ({CalcCountOfPages})";
					BeforePrinting();
					RecalcScale(bounds);
				}
				var oldTop = TopRow;
				var oldLeft = LeftColumn;
				CalcPageEndCheckHasMorePage(bounds, out var rightBottom);
				TopRow = oldTop;
				LeftColumn = oldLeft;
				ExecuteExpressions(new CellLocation() { Row = TopRow, Column = LeftColumn }, rightBottom);

				if (doc.PrinterSettings.PrintRange == PrintRange.SomePages)
				{
					if (CountOfPrintedPages < doc.PrinterSettings.FromPage)
					{
						do
						{
							e.HasMorePages = CalcPageEndCheckHasMorePage(bounds, out rightBottom);
							ExecuteExpressions(new CellLocation() { Row = TopRow, Column = LeftColumn }, rightBottom);
						}
						while (CountOfPrintedPages < doc.PrinterSettings.FromPage);
					}
					if (CountOfPrintedPages >= doc.PrinterSettings.ToPage)
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
	}
}
