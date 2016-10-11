using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CellReport.Presenters.WinForms
{
    internal partial class DoubleBufferedControl : UserControl
    {
        public DoubleBufferedControl()
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.Selectable | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ProcessKey != null)
                ProcessKey(this, new KeyEventArgs(keyData));

            return true;// base.ProcessCmdKey(ref msg, keyData);
        }

        public event EventHandler ProcessKey;
    }
}
