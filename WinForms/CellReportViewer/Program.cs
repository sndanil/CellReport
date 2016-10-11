using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CellReportViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0 || !System.IO.File.Exists(args[0]))
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Xml files (*.xml)|*.xml|CellReport templates (*.tcr)|*.tcr|CellReport templates package (*.tcrp)|*.tcrp|All files (*.*)|*.*";
                    dialog.FilterIndex = 3;
                    if (dialog.ShowDialog() == DialogResult.OK)
                        MainForm.FileName = dialog.FileName;
                    else
                        return;
                }
            }
            else
                MainForm.FileName = args[0];

            Application.Run(new MainForm());
        }
    }
}
