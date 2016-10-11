using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CellReport.WinForms.Presenters.WinForms
{
    public partial class GoToRowForm : Form
    {
        public GoToRowForm()
        {
            InitializeComponent();
        }

        private void _cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        public int SelectedRow
        {
            get { return Convert.ToInt32(_rowNumber.Value); }
            set { _rowNumber.Value = value; }
        }

        public int MaxSelectedRow
        {
            get { return Convert.ToInt32(_rowNumber.Maximum); }
            set { _rowNumber.Maximum = value; }
        }

        private void _okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            _rowNumber.Select(0, _rowNumber.Value.ToString().Length);
        }
    }
}
