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
    public class PrintPresenetr: BasePrintPresenter
    {
        public PrintPresenetr(CellManager manager)
            : base(manager)
        {
            Renderer = new GdiPlusRenderer(this);
            CurrentUnit = CellReport.Core.GraphicsUnit.Display;
        }

        public PrintDocument GetDocument()
        {
            base.BeforePrinting();
            PrintDocument doc = new PrintDocument();
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

        protected override void OnPrint(bool useCustom)
        {
            PrintDocument doc = GetDocument();
            if (useCustom)
            {
                using (var dialog = new System.Windows.Forms.PrintDialog())
                {
                    //dialog.AllowSomePages = true;
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
            ExecuteExpressions();

            DeviceDpiX = e.Graphics.DpiX;
            DeviceDpiY = e.Graphics.DpiY;

            var bounds = e.MarginBounds.ToCellRecttangle();
            bounds.X = Math.Max(e.PageSettings.HardMarginX, e.PageSettings.Margins.Left - e.PageSettings.HardMarginX);
            bounds.Y = Math.Max(e.PageSettings.HardMarginY, e.PageSettings.Margins.Top - e.PageSettings.HardMarginY);

            RecalcScale(bounds);
            bounds = RenderHeaderAndFooter(e.Graphics, bounds);
            e.Graphics.SetClip(new RectangleF(bounds.X, bounds.Y, GetCurrentPageWidth(bounds), bounds.Height));
            e.HasMorePages = RenderPage(e.Graphics, bounds);
        }

        protected override CellReport.Core.Size GetTextSize(string text, string fontFamily, float fontSize)
        {
            return GdiPlusRenderer.GetTextSize(text, fontFamily, fontSize);
        }
    }
}
