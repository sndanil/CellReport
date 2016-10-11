using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using CellReport.Core;

namespace CellReport.Presenters.WinForms
{
    internal partial class PresentControl : UserControl
    {
        private DisplayPresenter _presenter = null;

        public PresentControl()
        {
            InitializeComponent();

            _paintPanel.MouseWheel += new MouseEventHandler(PaintPanelMouseWheel);
            _paintPanel.MouseClick += new MouseEventHandler(PaintPanelMouseClick);
            _paintPanel.ProcessKey += new EventHandler(PaintPanelProcessKey);
            _paintPanel.DoubleClick += new EventHandler(PaintPanelDoubleClick);

            Application.Idle += new EventHandler(Application_Idle);
        }

        void Application_Idle(object sender, EventArgs e)
        {
            _showGridButton.Checked = _showGridMenuItem.Checked = _presenter.ShowGridLines;
            _showResizersButton.Checked = _showResizersMenuItem.Checked = _presenter.ShowResizers;
            _autoWidthMenuItem.Checked = _autoWidthCntxMenuItem.Checked = _presenter.Manager.PrintSettings.AutoFitWidthPages != 0;
        }

        public PresentControl(DisplayPresenter presenter)
            : this()
        {
            _presenter = presenter;
        }

        protected override void OnLoad(EventArgs e)
        {
            _scaleComboBox.SelectedIndex = _scaleComboBox.Items.IndexOf("100%");
            _columnResizer.Presenter = _presenter;
            _rowsResizer.TopOffset = _columnResizer.Height;
            _rowsResizer.Presenter = _presenter;
            _presenter.LeftColumnChanged += new EventHandler(TopRowLeftColumnChanged);
            _presenter.TopRowChanged += new EventHandler(TopRowLeftColumnChanged);
            _presenter.CurrentCellChanged += new EventHandler(TopRowLeftColumnChanged);
            _presenter.ShowGridLinesChanged += new EventHandler(TopRowLeftColumnChanged);
            _presenter.ShowResizersChanged += new EventHandler(TopRowLeftColumnChanged);
            _presenter.ShowToolBarChanged += new EventHandler(TopRowLeftColumnChanged);
            _presenter.ScaleChanged += new EventHandler(TopRowLeftColumnChanged);
            _paintPanel.Paint += new PaintEventHandler(_presenter.Control_Paint);
            _paintPanel.Focus();

            Update(false);
        }

        public void Update(bool invalidate)
        {
            _columnResizer.Visible = _presenter.ShowResizers;
            _rowsResizer.Visible = _presenter.ShowResizers;
            _toolBar.Visible = _presenter.ShowToolBar;
            _hScrollBar.Maximum = _presenter.Manager.ColumnsCount;
            _vScrollBar.Maximum = _presenter.Manager.RowsCount;
            _vScrollBar.Value = _presenter.TopRow;
            _hScrollBar.Value = _presenter.LeftColumn;
            if (invalidate)
                this.Invalidate(true);
        }

        private void TopRowLeftColumnChanged(object sender, EventArgs e)
        {
            Update(true);
        }

        private void ScrollBar_ValueChanged(object sender, EventArgs e)
        {
            if (_presenter != null)
            {
                _presenter.TopRow = _vScrollBar.Value;
                _presenter.LeftColumn = _hScrollBar.Value;
            }
        }

        private void ShowGridButtonClick(object sender, EventArgs e)
        {
            _presenter.ShowGridLines = !_presenter.ShowGridLines;
        }

        private void ShowResizersButtonClick(object sender, EventArgs e)
        {
            _presenter.ShowResizers = !_presenter.ShowResizers;
        }

        private void PrintButtonClick(object sender, EventArgs e)
        {
            var printPresenter = new PrintPresenetr(_presenter.Manager);
            printPresenter.Print();
        }

        private void CustomPrintMenuItemClick(object sender, EventArgs e)
        {
            var printPresenter = new PrintPresenetr(_presenter.Manager);
            printPresenter.CustomPrint();
        }

        private void PageSetupMenuItemClick(object sender, EventArgs e)
        {
            var printPresenter = new PrintPresenetr(_presenter.Manager);
            printPresenter.ShowCustomPage();
        }

        private void PrintPreviewMenuItemClick(object sender, EventArgs e)
        {
            var printPresenter = new PrintPresenetr(_presenter.Manager);
            printPresenter.ShowPreviewDialog();
        }

        private void AutoWidthMenuItemClick(object sender, EventArgs e)
        {
            _presenter.Manager.PrintSettings.AutoFitWidthPages =
                _presenter.Manager.PrintSettings.AutoFitWidthPages == 0 ? 1 : 0;
        }

        private void ScaleComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            var scaleStr = string.IsNullOrEmpty(_scaleComboBox.Text) ? "100" : _scaleComboBox.Text.Replace("%", string.Empty);
            _presenter.Scale = float.Parse(scaleStr, CultureInfo.InvariantCulture) / 100f;
            _paintPanel.Focus();
        }

        private void PaintPanelMouseWheel(object sender, MouseEventArgs e)
        {
            if (_presenter != null)
            {
                int numberToGo = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
                _presenter.TopRow = _presenter.TopRow - numberToGo;
            }
        }

        private void PaintPanelMouseClick(object sender, MouseEventArgs e)
        {
            _presenter.RaiseClick(sender, e, e.X, e.Y);
        }

        private void PaintPanelDoubleClick(object sender, EventArgs e)
        {
            _presenter.RaiseDblClick(sender, e, _paintPanel.PointToClient(MousePosition).X, 
                _paintPanel.PointToClient(MousePosition).Y);
        }

        void PaintPanelProcessKey(object sender, EventArgs e)
        {
            _presenter.RaiseProcessKey(sender, e);
        }

        internal RectangleF GetPaintPanelBounds()
        {
            return _paintPanel.Bounds;
        }

        private void ExportButtonClick(object sender, EventArgs e)
        {
            var exp = new Export.ExcelExporter(_presenter.Manager);
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter =string.Format("{0} файлы (*.{1})|*.{1}|Все файлы (*.*)|*.*", exp.Name, exp.Extension);
                dialog.FilterIndex = 1;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    using (var file = System.IO.File.Create(dialog.FileName))
                        exp.ExportTo(file);
                }
            }
        }

        private void SaveMenuItemClick(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CellReport шаблон (*.tcr)|*.tcr|CellReport шаблон пакетный (*.tcrp)|*.tcrp|All files (*.*)|*.*";
                dialog.FilterIndex = 1;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    (new CellTemplate(_presenter.Manager))
                        .Save(dialog.FileName, dialog.FileName.EndsWith("tcrp") ? TemplateType.Package : TemplateType.Xml);
                }
            }
        }
    }
}
