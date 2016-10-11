using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CellReportViewer
{
    public partial class MainForm : Form
    {
        public static string FileName = null;

        public MainForm()
        {
            InitializeComponent();

            var template = new CellReport.Core.CellTemplate();
            template.Load(FileName);
            new CellReport.Presenters.WinForms.DisplayPresenter(template.Manager, this);
        }
    }
}
