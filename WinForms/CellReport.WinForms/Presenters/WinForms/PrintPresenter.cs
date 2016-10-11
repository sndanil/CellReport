using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellReport.Core;
using System.Drawing.Printing;
using System.Drawing;
using System.Windows.Forms;
using CellReport.Core.Contents;

namespace CellReport.Presenters.WinForms
{
    public class PrintPresenter: BasePrintPresenter
    {
        private PrintAction _action = PrintAction.PrintToPrinter;
        private float _hardMarginX = 0;
        private float _hardMarginY = 0;

        public PrintPresenter(CellManager manager)
            : base(manager)
        {
            Renderer = new GdiPlusRenderer();
            CurrentUnit = CellReport.Core.GraphicsUnit.Display;
        }

        public PrintDocument GetDocument()
        {
            base.BeforePrinting();
            PrintDocument doc = new PrintDocument();
            doc.BeginPrint += new PrintEventHandler(doc_BeginPrint);
            doc.DefaultPageSettings.Landscape = Manager.PrintSettings.Landscape;
            doc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins() {
                Left = Convert.ToInt32(UnitsConverter.ConvertValue(Manager.PrintSettings.Margins.Left, CellReport.Core.GraphicsUnit.Millimeter, CellReport.Core.GraphicsUnit.Display, 0)),
                Top = Convert.ToInt32(UnitsConverter.ConvertValue(Manager.PrintSettings.Margins.Top, CellReport.Core.GraphicsUnit.Millimeter, CellReport.Core.GraphicsUnit.Display, 0)),
                Right = Convert.ToInt32(UnitsConverter.ConvertValue(Manager.PrintSettings.Margins.Right, CellReport.Core.GraphicsUnit.Millimeter, CellReport.Core.GraphicsUnit.Display, 0)),
                Bottom = Convert.ToInt32(UnitsConverter.ConvertValue(Manager.PrintSettings.Margins.Bottom, CellReport.Core.GraphicsUnit.Millimeter, CellReport.Core.GraphicsUnit.Display, 0))
            };
            doc.DocumentName = Manager.Properties.DocumentName;
            doc.PrintPage += new PrintPageEventHandler(DocPrintPage);

            return doc;
        }

        void doc_BeginPrint(object sender, PrintEventArgs e)
        {
            _action = e.PrintAction;
        }

        protected override void OnPrint(bool useCustom)
        {
            PrintDocument doc = GetDocument();
            if (useCustom)
            {
                using (var dialog = new System.Windows.Forms.PrintDialog())
                {
                    dialog.AllowSomePages = true;
                    dialog.UseEXDialog = true;
                    dialog.Document = doc;
                    if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
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
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Manager.PrintSettings.Landscape = doc.DefaultPageSettings.Landscape;
                    Manager.PrintSettings.Margins = new CellReport.Core.Margins(
                        UnitsConverter.ConvertValue(doc.DefaultPageSettings.Margins.Left, CellReport.Core.GraphicsUnit.Display, CellReport.Core.GraphicsUnit.Millimeter, 0),
                        UnitsConverter.ConvertValue(doc.DefaultPageSettings.Margins.Right, CellReport.Core.GraphicsUnit.Display, CellReport.Core.GraphicsUnit.Millimeter, 0),
                        UnitsConverter.ConvertValue(doc.DefaultPageSettings.Margins.Top, CellReport.Core.GraphicsUnit.Display, CellReport.Core.GraphicsUnit.Millimeter, 0),
                        UnitsConverter.ConvertValue(doc.DefaultPageSettings.Margins.Bottom, CellReport.Core.GraphicsUnit.Display, CellReport.Core.GraphicsUnit.Millimeter, 0)
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
            this.BeginRender();
            e.HasMorePages = true;
            PrintDocument doc = sender as PrintDocument;
            if (doc == null)
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
                    bounds.X = Math.Max(_hardMarginX, e.PageSettings.Margins.Left - _hardMarginX);
                    bounds.Y = Math.Max(_hardMarginY, e.PageSettings.Margins.Top - _hardMarginY);
                }
                var defaultBounds = bounds;

                if (CountOfPrintedPages == 0)
                {
                    RecalcScale(bounds);
                    CalcCountOfPages = CalcPages(bounds);
                    doc.DocumentName = string.Format("{0} ({1})", Manager.Properties.DocumentName, CalcCountOfPages);
                    BeforePrinting();
                    RecalcScale(bounds);
                }
                CellLocation rightBottom;
                var oldTop = TopRow;
                var oldLeft = LeftColumn;
                CalcPageEndCheckHasMorePage(bounds, out rightBottom);
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
                this.EndRender();
            }
        }

        protected override CellReport.Core.Size GetTextSize(string text, string fontFamily, float fontSize, CellReport.Core.FontStyle fontStyle)
        {
            return GdiPlusRenderer.GetTextSize(text, fontFamily, fontSize, fontStyle);
        }
    }
}
