using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CellReport.Presenters.WinForms
{
    internal partial class PreviewForm : Form
    {
        PrintPresenetr _presenter = null;

        public PreviewForm()
        {
            InitializeComponent();

            Application.Idle += new EventHandler(Application_Idle);
        }

        void Application_Idle(object sender, EventArgs e)
        {
            _autoWidthButton.Checked = _presenter != null && _presenter.Manager.PrintSettings.AutoFitWidthPages > 0;
            _prevPageButton.Enabled = _firstPageButton.Enabled = _previewControl.StartPage > 0;
            _nextPageButton.Enabled = _lastPageButton.Enabled = _previewControl.StartPage != _presenter.CountOfPages - 1;            
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Idle -= new EventHandler(Application_Idle);
        }

        public void SetPresenter(PrintPresenetr presenter)
        {
            _presenter = presenter;
            UpdateDocument();
            Text = string.Format("{0} [ {1} ]", Text, _presenter.Manager.Properties.DocumentName);
        }

        private void UpdateDocument()
        {
            this.Controls.Remove(_previewControl);
            _previewControl.Dispose();
            _previewControl = new PrintPreviewControl();
            this.Controls.Add(_previewControl);
            _previewControl.Dock = DockStyle.Fill;
            _previewControl.BringToFront();
            _previewControl.Document = _presenter.GetDocument();
            UpdateCurrentPage();
        }

        private void _printButton_Click(object sender, EventArgs e)
        {
            _presenter.Print();
        }

        private void _customPrintButton_Click(object sender, EventArgs e)
        {
            _presenter.CustomPrint();
        }

        private void _customPageButton_Click(object sender, EventArgs e)
        {
            _presenter.ShowCustomPage();
            UpdateDocument();
        }

        private void _autoWidthButton_Click(object sender, EventArgs e)
        {
            _presenter.Manager.PrintSettings.AutoFitWidthPages =
                _presenter.Manager.PrintSettings.AutoFitWidthPages == 0 ? 1 : 0;
            UpdateDocument();
        }

        private void UpdateCurrentPage()
        {
            _currentPageTextBox.Text = (_previewControl.StartPage + 1).ToString();
        }

        private void _firstPageButton_Click(object sender, EventArgs e)
        {
            _previewControl.StartPage = 0;
            UpdateCurrentPage();
        }

        private void _prevPageButton_Click(object sender, EventArgs e)
        {
            _previewControl.StartPage = _previewControl.StartPage > 0 ? _previewControl.StartPage - 1 : _previewControl.StartPage;
            UpdateCurrentPage();
        }

        private void _nextPageButton_Click(object sender, EventArgs e)
        {
            _previewControl.StartPage = _previewControl.StartPage < (_presenter.CountOfPages - 1) ? _previewControl.StartPage + 1 : _previewControl.StartPage;
            UpdateCurrentPage();
        }

        private void _lastPageButton_Click(object sender, EventArgs e)
        {
            _previewControl.StartPage = (_presenter.CountOfPages - 1);
            UpdateCurrentPage();
        }
    }
}
