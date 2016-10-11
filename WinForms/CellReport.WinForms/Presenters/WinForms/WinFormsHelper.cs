using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CellReport.Presenters.WinForms
{
    public static class WinFormsHelper
    {
        public static DisplayPresenter Show(this ReportDocument report, Form mdiParent, bool autoFit)
        {
            if (autoFit)
                report.FitDocument();
            var presenter = new DisplayPresenter(report.Manager, new ReportForm() 
            { 
                MdiParent = mdiParent, 
                Text = report.Manager.Properties.DocumentName 
            });
            presenter.Control.Show();
            return presenter;
        }

        public static DisplayPresenter Show(this ReportDocument report, Form mdiParent)
        {
            return report.Show(mdiParent, true);
        }

        public static DisplayPresenter Show(this ReportDocument report)
        {
            return report.Show(null, true);
        }

        public static DisplayPresenter Show(this CellReport.Core.CellTemplate template, Form mdiParent)
        {
            var presenter = new DisplayPresenter(template.Manager, new ReportForm()
            {
                MdiParent = mdiParent,
                Text = template.Manager.Properties.DocumentName
            });
            presenter.Control.Show();

            return presenter;
        }

    }
}
