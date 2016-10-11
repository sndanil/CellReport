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
        private class ZoomInfo
        {
            public bool IsAuto { get; set; }
            public string Name { get; set; }
            public double Scale { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        PrintPresenter _presenter = null;
        private ComboBox _scaleComboBox = new ComboBox();

        public PreviewForm()
        {
            InitializeComponent();

            Application.Idle += new EventHandler(Application_Idle);
            _scaleComboBox.Width = 100;
            _scaleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _scaleComboBox.Items.Add(new ZoomInfo { Name = "Авто", IsAuto = true, Scale = 1 }); ;
            _scaleComboBox.Items.Add(new ZoomInfo { Name = "25%", Scale = 0.25 });
            _scaleComboBox.Items.Add(new ZoomInfo { Name = "50%", Scale = 0.5 });
            _scaleComboBox.Items.Add(new ZoomInfo { Name = "75%", Scale = 0.75 });
            _scaleComboBox.Items.Add(new ZoomInfo { Name = "100%", Scale = 1 });
            _scaleComboBox.Items.Add(new ZoomInfo { Name = "150%", Scale = 1.5 });
            _scaleComboBox.Items.Add(new ZoomInfo { Name = "200%", Scale = 2 });
            _scaleComboBox.Items.Add(new ZoomInfo { Name = "250%", Scale = 2.5 });
            _scaleComboBox.SelectedIndex = 0;
            _scaleComboBox.SelectedIndexChanged += new EventHandler(CcomboBoxSelectedIndexChanged);
            var host = new ToolStripControlHost(_scaleComboBox);
            _previewToolStrip.Items.Add(host);
        }

        void CcomboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            var zoomInfo = _scaleComboBox.SelectedItem as ZoomInfo;
            if (zoomInfo != null)
            {
                _previewControl.Zoom = zoomInfo.Scale;
                _previewControl.AutoZoom = zoomInfo.IsAuto;
                _previewControl.Focus();
            }
        }

        void Application_Idle(object sender, EventArgs e)
        {
            _autoWidthButton.Checked = _presenter != null && _presenter.Manager.PrintSettings.AutoFitWidthPages > 0;
            _prevPageButton.Enabled = _firstPageButton.Enabled = _previewControl.StartPage > 0;
            _nextPageButton.Enabled = _lastPageButton.Enabled = _previewControl.StartPage != _presenter.CountOfPrintedPages - 1;            
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Idle -= new EventHandler(Application_Idle);
        }

        public void SetPresenter(PrintPresenter presenter)
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
            CcomboBoxSelectedIndexChanged(this, EventArgs.Empty);
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
            _previewControl.StartPage = _previewControl.StartPage < (_presenter.CountOfPrintedPages - 1) ? _previewControl.StartPage + 1 : _previewControl.StartPage;
            UpdateCurrentPage();
        }

        private void _lastPageButton_Click(object sender, EventArgs e)
        {
            _previewControl.StartPage = (_presenter.CountOfPrintedPages - 1);
            UpdateCurrentPage();
        }
    }
}
