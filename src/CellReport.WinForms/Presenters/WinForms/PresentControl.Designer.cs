using System.Windows.Forms;
using System;
namespace CellReport.Presenters.WinForms
{
    partial class PresentControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
            Application.Idle -= new EventHandler(Application_Idle);
            _moveTimer.Dispose();
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _vScrollBar = new VScrollBar();
            _hScrollBar = new HScrollBar();
            _gridPanel = new Panel();
            _paintPanel = new DoubleBufferedControl();
            _contextMenu = new ContextMenuStrip(components);
            _printCntxMenuItem = new ToolStripMenuItem();
            _customPrintCntxMenuItem = new ToolStripMenuItem();
            _previewCntxMenuItem = new ToolStripMenuItem();
            _customPageCntxMenuItem = new ToolStripMenuItem();
            _autoWidthCntxMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            _showGridMenuItem = new ToolStripMenuItem();
            _showResizersMenuItem = new ToolStripMenuItem();
            _goToRowMenuItem = new ToolStripMenuItem();
            _findMenuItem = new ToolStripMenuItem();
            _exportExcelMenuItem = new ToolStripMenuItem();
            _exportPdfMenuItem = new ToolStripMenuItem();
            _saveMenuItem = new ToolStripMenuItem();
            _columnResizer = new ColumnResizerControl();
            _rowsResizer = new RowResizerControl();
            _bookMarksSplitter = new Splitter();
            _bookMarksTreeView = new TreeView();
            _bookMarksImageList = new ImageList(components);
            _toolBar = new ToolStrip();
            _printButton = new ToolStripSplitButton();
            _customPrintMenuItem = new ToolStripMenuItem();
            _printPreviewMenuItem = new ToolStripMenuItem();
            _pageSetupMenuItem = new ToolStripMenuItem();
            _autoWidthMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            _showGridButton = new ToolStripButton();
            _showResizersButton = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            _scaleComboBox = new ToolStripComboBox();
            toolStripSeparator4 = new ToolStripSeparator();
            _excelExportButton = new ToolStripButton();
            _pdfExportButton = new ToolStripButton();
            toolStripSeparator5 = new ToolStripSeparator();
            _findStripButton = new ToolStripButton();
            _bookMarksStripButton = new ToolStripButton();
            _gridPanel.SuspendLayout();
            _contextMenu.SuspendLayout();
            _toolBar.SuspendLayout();
            SuspendLayout();
            // 
            // _vScrollBar
            // 
            _vScrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            _vScrollBar.LargeChange = 5;
            _vScrollBar.Location = new System.Drawing.Point(1118, 0);
            _vScrollBar.Name = "_vScrollBar";
            _vScrollBar.Size = new System.Drawing.Size(17, 1055);
            _vScrollBar.TabIndex = 0;
            _vScrollBar.ValueChanged += ScrollBar_ValueChanged;
            // 
            // _hScrollBar
            // 
            _hScrollBar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _hScrollBar.LargeChange = 5;
            _hScrollBar.Location = new System.Drawing.Point(0, 1055);
            _hScrollBar.Name = "_hScrollBar";
            _hScrollBar.Size = new System.Drawing.Size(1118, 17);
            _hScrollBar.TabIndex = 1;
            _hScrollBar.ValueChanged += ScrollBar_ValueChanged;
            // 
            // _gridPanel
            // 
            _gridPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _gridPanel.Controls.Add(_paintPanel);
            _gridPanel.Controls.Add(_columnResizer);
            _gridPanel.Controls.Add(_rowsResizer);
            _gridPanel.Controls.Add(_bookMarksSplitter);
            _gridPanel.Controls.Add(_bookMarksTreeView);
            _gridPanel.Controls.Add(_toolBar);
            _gridPanel.Location = new System.Drawing.Point(0, 0);
            _gridPanel.Margin = new Padding(6, 7, 6, 7);
            _gridPanel.Name = "_gridPanel";
            _gridPanel.Size = new System.Drawing.Size(1118, 1055);
            _gridPanel.TabIndex = 3;
            // 
            // _paintPanel
            // 
            _paintPanel.BackColor = System.Drawing.Color.White;
            _paintPanel.ContextMenuStrip = _contextMenu;
            _paintPanel.Dock = DockStyle.Fill;
            _paintPanel.Location = new System.Drawing.Point(422, 82);
            _paintPanel.Margin = new Padding(6, 7, 6, 7);
            _paintPanel.Name = "_paintPanel";
            _paintPanel.Size = new System.Drawing.Size(696, 973);
            _paintPanel.TabIndex = 2;
            // 
            // _contextMenu
            // 
            _contextMenu.ImageScalingSize = new System.Drawing.Size(28, 28);
            _contextMenu.Items.AddRange(new ToolStripItem[] { _printCntxMenuItem, toolStripSeparator2, _showGridMenuItem, _showResizersMenuItem, _goToRowMenuItem, _findMenuItem, _exportExcelMenuItem, _exportPdfMenuItem, _saveMenuItem });
            _contextMenu.Name = "_contextMenu";
            _contextMenu.RenderMode = ToolStripRenderMode.System;
            _contextMenu.Size = new System.Drawing.Size(349, 298);
            // 
            // _printCntxMenuItem
            // 
            _printCntxMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _customPrintCntxMenuItem, _previewCntxMenuItem, _customPageCntxMenuItem, _autoWidthCntxMenuItem });
            _printCntxMenuItem.Image = CellReport.WinForms.ToolbarIcons.Print;
            _printCntxMenuItem.Name = "_printCntxMenuItem";
            _printCntxMenuItem.Size = new System.Drawing.Size(348, 36);
            _printCntxMenuItem.Text = "#Report/Print";
            _printCntxMenuItem.Click += PrintButtonClick;
            // 
            // _customPrintCntxMenuItem
            // 
            _customPrintCntxMenuItem.Image = CellReport.WinForms.ToolbarIcons.Print;
            _customPrintCntxMenuItem.Name = "_customPrintCntxMenuItem";
            _customPrintCntxMenuItem.ShortcutKeys = Keys.Control | Keys.P;
            _customPrintCntxMenuItem.Size = new System.Drawing.Size(397, 40);
            _customPrintCntxMenuItem.Text = "#Report/CustomPrint";
            _customPrintCntxMenuItem.Click += CustomPrintMenuItemClick;
            // 
            // _previewCntxMenuItem
            // 
            _previewCntxMenuItem.Image = CellReport.WinForms.ToolbarIcons.PrintPreview;
            _previewCntxMenuItem.Name = "_previewCntxMenuItem";
            _previewCntxMenuItem.Size = new System.Drawing.Size(397, 40);
            _previewCntxMenuItem.Text = "#Report/Preview";
            _previewCntxMenuItem.Click += PrintPreviewMenuItemClick;
            // 
            // _customPageCntxMenuItem
            // 
            _customPageCntxMenuItem.Image = CellReport.WinForms.ToolbarIcons.PrintSetup;
            _customPageCntxMenuItem.Name = "_customPageCntxMenuItem";
            _customPageCntxMenuItem.Size = new System.Drawing.Size(397, 40);
            _customPageCntxMenuItem.Text = "#Report/PageSettings";
            _customPageCntxMenuItem.Click += PageSetupMenuItemClick;
            // 
            // _autoWidthCntxMenuItem
            // 
            _autoWidthCntxMenuItem.Name = "_autoWidthCntxMenuItem";
            _autoWidthCntxMenuItem.Size = new System.Drawing.Size(397, 40);
            _autoWidthCntxMenuItem.Text = "#Report/AutoFitPage";
            _autoWidthCntxMenuItem.Click += AutoWidthMenuItemClick;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(345, 6);
            // 
            // _showGridMenuItem
            // 
            _showGridMenuItem.Image = CellReport.WinForms.ToolbarIcons.ShowGrid16x16;
            _showGridMenuItem.Name = "_showGridMenuItem";
            _showGridMenuItem.Size = new System.Drawing.Size(348, 36);
            _showGridMenuItem.Text = "#Report/ShowHideGrid";
            _showGridMenuItem.Click += ShowGridButtonClick;
            // 
            // _showResizersMenuItem
            // 
            _showResizersMenuItem.Image = CellReport.WinForms.ToolbarIcons.ShowColumns16x16;
            _showResizersMenuItem.Name = "_showResizersMenuItem";
            _showResizersMenuItem.Size = new System.Drawing.Size(348, 36);
            _showResizersMenuItem.Text = "#Report/ShowHideHeaders";
            _showResizersMenuItem.Click += ShowResizersButtonClick;
            // 
            // _goToRowMenuItem
            // 
            _goToRowMenuItem.Name = "_goToRowMenuItem";
            _goToRowMenuItem.ShortcutKeys = Keys.Control | Keys.G;
            _goToRowMenuItem.Size = new System.Drawing.Size(348, 36);
            _goToRowMenuItem.Text = "#Report/GoToLine";
            _goToRowMenuItem.Click += GoToRowMenuItemClick;
            // 
            // _findMenuItem
            // 
            _findMenuItem.Image = CellReport.WinForms.ToolbarIcons.Find;
            _findMenuItem.Name = "_findMenuItem";
            _findMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            _findMenuItem.Size = new System.Drawing.Size(348, 36);
            _findMenuItem.Text = "#Report/Search";
            _findMenuItem.Click += FindMenuItemClick;
            // 
            // _exportExcelMenuItem
            // 
            _exportExcelMenuItem.Image = CellReport.WinForms.ToolbarIcons.Excel2013_16;
            _exportExcelMenuItem.Name = "_exportExcelMenuItem";
            _exportExcelMenuItem.Size = new System.Drawing.Size(348, 36);
            _exportExcelMenuItem.Text = "#Report/ExportToExcel";
            _exportExcelMenuItem.Click += ExportExcelButtonClick;
            // 
            // _exportPdfMenuItem
            // 
            _exportPdfMenuItem.Image = CellReport.WinForms.ToolbarIcons.Pdf2616_32;
            _exportPdfMenuItem.Name = "_exportPdfMenuItem";
            _exportPdfMenuItem.Size = new System.Drawing.Size(348, 36);
            _exportPdfMenuItem.Text = "#Report/ExportToPdf";
            _exportPdfMenuItem.Click += ExportPdfButtonClick;
            // 
            // _saveMenuItem
            // 
            _saveMenuItem.Image = CellReport.WinForms.ToolbarIcons.Save;
            _saveMenuItem.Name = "_saveMenuItem";
            _saveMenuItem.Size = new System.Drawing.Size(348, 36);
            _saveMenuItem.Text = "#Report/Save";
            _saveMenuItem.Click += SaveMenuItemClick;
            // 
            // _columnResizer
            // 
            _columnResizer.BackColor = System.Drawing.SystemColors.ActiveBorder;
            _columnResizer.Dock = DockStyle.Top;
            _columnResizer.ForeColor = System.Drawing.Color.FromArgb(39, 65, 62);
            _columnResizer.Location = new System.Drawing.Point(422, 40);
            _columnResizer.Margin = new Padding(12, 16, 12, 16);
            _columnResizer.Name = "_columnResizer";
            _columnResizer.Presenter = null;
            _columnResizer.Size = new System.Drawing.Size(696, 42);
            _columnResizer.TabIndex = 3;
            _columnResizer.TabStop = false;
            // 
            // _rowsResizer
            // 
            _rowsResizer.BackColor = System.Drawing.SystemColors.ActiveBorder;
            _rowsResizer.Dock = DockStyle.Left;
            _rowsResizer.ForeColor = System.Drawing.Color.FromArgb(39, 65, 62);
            _rowsResizer.Location = new System.Drawing.Point(342, 40);
            _rowsResizer.Margin = new Padding(6, 7, 6, 7);
            _rowsResizer.Name = "_rowsResizer";
            _rowsResizer.Presenter = null;
            _rowsResizer.Size = new System.Drawing.Size(80, 1015);
            _rowsResizer.TabIndex = 4;
            _rowsResizer.TabStop = false;
            _rowsResizer.TopOffset = 0;
            // 
            // _bookMarksSplitter
            // 
            _bookMarksSplitter.Location = new System.Drawing.Point(336, 40);
            _bookMarksSplitter.Margin = new Padding(6, 7, 6, 7);
            _bookMarksSplitter.Name = "_bookMarksSplitter";
            _bookMarksSplitter.Size = new System.Drawing.Size(6, 1015);
            _bookMarksSplitter.TabIndex = 7;
            _bookMarksSplitter.TabStop = false;
            _bookMarksSplitter.Visible = false;
            // 
            // _bookMarksTreeView
            // 
            _bookMarksTreeView.Dock = DockStyle.Left;
            _bookMarksTreeView.ImageIndex = 0;
            _bookMarksTreeView.ImageList = _bookMarksImageList;
            _bookMarksTreeView.Location = new System.Drawing.Point(0, 40);
            _bookMarksTreeView.Margin = new Padding(12, 7, 12, 7);
            _bookMarksTreeView.Name = "_bookMarksTreeView";
            _bookMarksTreeView.SelectedImageIndex = 0;
            _bookMarksTreeView.Size = new System.Drawing.Size(336, 1015);
            _bookMarksTreeView.TabIndex = 6;
            _bookMarksTreeView.Visible = false;
            _bookMarksTreeView.AfterSelect += BookMarksTreeViewAfterSelect;
            // 
            // _bookMarksImageList
            // 
            _bookMarksImageList.ColorDepth = ColorDepth.Depth32Bit;
            _bookMarksImageList.ImageSize = new System.Drawing.Size(16, 16);
            _bookMarksImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // _toolBar
            // 
            _toolBar.GripStyle = ToolStripGripStyle.Hidden;
            _toolBar.ImageScalingSize = new System.Drawing.Size(28, 28);
            _toolBar.Items.AddRange(new ToolStripItem[] { _printButton, toolStripSeparator1, _showGridButton, _showResizersButton, toolStripSeparator3, _scaleComboBox, toolStripSeparator4, _excelExportButton, _pdfExportButton, toolStripSeparator5, _findStripButton, _bookMarksStripButton });
            _toolBar.Location = new System.Drawing.Point(0, 0);
            _toolBar.Name = "_toolBar";
            _toolBar.Padding = new Padding(0, 0, 4, 0);
            _toolBar.RenderMode = ToolStripRenderMode.System;
            _toolBar.Size = new System.Drawing.Size(1118, 40);
            _toolBar.TabIndex = 5;
            // 
            // _printButton
            // 
            _printButton.DropDownItems.AddRange(new ToolStripItem[] { _customPrintMenuItem, _printPreviewMenuItem, _pageSetupMenuItem, _autoWidthMenuItem });
            _printButton.Image = CellReport.WinForms.ToolbarIcons.Print;
            _printButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _printButton.Name = "_printButton";
            _printButton.Size = new System.Drawing.Size(189, 34);
            _printButton.Text = "#Report/Print";
            _printButton.ButtonClick += PrintButtonClick;
            // 
            // _customPrintMenuItem
            // 
            _customPrintMenuItem.Image = CellReport.WinForms.ToolbarIcons.Print;
            _customPrintMenuItem.Name = "_customPrintMenuItem";
            _customPrintMenuItem.Size = new System.Drawing.Size(331, 40);
            _customPrintMenuItem.Text = "#Report/CustomPrint";
            _customPrintMenuItem.Click += CustomPrintMenuItemClick;
            // 
            // _printPreviewMenuItem
            // 
            _printPreviewMenuItem.Image = CellReport.WinForms.ToolbarIcons.PrintPreview;
            _printPreviewMenuItem.Name = "_printPreviewMenuItem";
            _printPreviewMenuItem.Size = new System.Drawing.Size(331, 40);
            _printPreviewMenuItem.Text = "#Report/Preview";
            _printPreviewMenuItem.Click += PrintPreviewMenuItemClick;
            // 
            // _pageSetupMenuItem
            // 
            _pageSetupMenuItem.Image = CellReport.WinForms.ToolbarIcons.PrintSetup;
            _pageSetupMenuItem.Name = "_pageSetupMenuItem";
            _pageSetupMenuItem.Size = new System.Drawing.Size(331, 40);
            _pageSetupMenuItem.Text = "#Report/PageSettings";
            _pageSetupMenuItem.Click += PageSetupMenuItemClick;
            // 
            // _autoWidthMenuItem
            // 
            _autoWidthMenuItem.Name = "_autoWidthMenuItem";
            _autoWidthMenuItem.Size = new System.Drawing.Size(331, 40);
            _autoWidthMenuItem.Text = "#Report/AutoFitPage";
            _autoWidthMenuItem.Click += AutoWidthMenuItemClick;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 40);
            // 
            // _showGridButton
            // 
            _showGridButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _showGridButton.Image = CellReport.WinForms.ToolbarIcons.ShowGrid16x16;
            _showGridButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _showGridButton.Name = "_showGridButton";
            _showGridButton.Size = new System.Drawing.Size(40, 34);
            _showGridButton.Text = "#Report/ShowHideGrid";
            _showGridButton.Click += ShowGridButtonClick;
            // 
            // _showResizersButton
            // 
            _showResizersButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _showResizersButton.Image = CellReport.WinForms.ToolbarIcons.ShowColumns16x16;
            _showResizersButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _showResizersButton.Name = "_showResizersButton";
            _showResizersButton.Size = new System.Drawing.Size(40, 34);
            _showResizersButton.Text = "#Report/ShowHideHeaders";
            _showResizersButton.Click += ShowResizersButtonClick;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new System.Drawing.Size(6, 40);
            // 
            // _scaleComboBox
            // 
            _scaleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _scaleComboBox.Items.AddRange(new object[] { "50%", "75%", "85%", "100%", "125%", "150%", "175%", "200%", "300%", "500%" });
            _scaleComboBox.Name = "_scaleComboBox";
            _scaleComboBox.Size = new System.Drawing.Size(146, 40);
            _scaleComboBox.SelectedIndexChanged += ScaleComboBoxSelectedIndexChanged;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new System.Drawing.Size(6, 40);
            // 
            // _excelExportButton
            // 
            _excelExportButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _excelExportButton.Image = CellReport.WinForms.ToolbarIcons.Excel2013_16;
            _excelExportButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _excelExportButton.Name = "_excelExportButton";
            _excelExportButton.Size = new System.Drawing.Size(40, 34);
            _excelExportButton.Text = "#Report/ExportToExcel";
            _excelExportButton.Click += ExportExcelButtonClick;
            // 
            // _pdfExportButton
            // 
            _pdfExportButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _pdfExportButton.Image = CellReport.WinForms.ToolbarIcons.Pdf2616_32;
            _pdfExportButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _pdfExportButton.Name = "_pdfExportButton";
            _pdfExportButton.Size = new System.Drawing.Size(40, 34);
            _pdfExportButton.Text = "#Report/ExportToPdf";
            _pdfExportButton.Click += ExportPdfButtonClick;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new System.Drawing.Size(6, 40);
            // 
            // _findStripButton
            // 
            _findStripButton.Image = CellReport.WinForms.ToolbarIcons.Find;
            _findStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _findStripButton.Name = "_findStripButton";
            _findStripButton.Size = new System.Drawing.Size(188, 34);
            _findStripButton.Text = "#Report/Search";
            _findStripButton.Click += FindMenuItemClick;
            // 
            // _bookMarksStripButton
            // 
            _bookMarksStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _bookMarksStripButton.Image = CellReport.WinForms.ToolbarIcons.Bookmark;
            _bookMarksStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _bookMarksStripButton.Name = "_bookMarksStripButton";
            _bookMarksStripButton.Size = new System.Drawing.Size(40, 34);
            _bookMarksStripButton.Text = "#Report/BookMarks";
            _bookMarksStripButton.Click += BookMarksStripButtonClick;
            // 
            // PresentControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_gridPanel);
            Controls.Add(_hScrollBar);
            Controls.Add(_vScrollBar);
            Margin = new Padding(6, 7, 6, 7);
            Name = "PresentControl";
            Size = new System.Drawing.Size(1152, 1094);
            _gridPanel.ResumeLayout(false);
            _gridPanel.PerformLayout();
            _contextMenu.ResumeLayout(false);
            _toolBar.ResumeLayout(false);
            _toolBar.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.VScrollBar _vScrollBar;
        private System.Windows.Forms.HScrollBar _hScrollBar;
        private System.Windows.Forms.Panel _gridPanel;
        private ColumnResizerControl _columnResizer;
        private RowResizerControl _rowsResizer;
        private DoubleBufferedControl _paintPanel;
        private System.Windows.Forms.ToolStrip _toolBar;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton _showGridButton;
        private System.Windows.Forms.ToolStripButton _showResizersButton;
        private System.Windows.Forms.ToolStripSplitButton _printButton;
        private System.Windows.Forms.ToolStripMenuItem _customPrintMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _printPreviewMenuItem;
        private ToolStripMenuItem _pageSetupMenuItem;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _printCntxMenuItem;
        private ToolStripMenuItem _customPrintCntxMenuItem;
        private ToolStripMenuItem _previewCntxMenuItem;
        private ToolStripMenuItem _customPageCntxMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem _showGridMenuItem;
        private ToolStripMenuItem _showResizersMenuItem;
        private ToolStripMenuItem _autoWidthMenuItem;
        private ToolStripMenuItem _autoWidthCntxMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripComboBox _scaleComboBox;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripMenuItem _saveMenuItem;
        private ToolStripButton _excelExportButton;
        private ToolStripButton _pdfExportButton;
        private ToolStripMenuItem _exportExcelMenuItem;
        private ToolStripMenuItem _exportPdfMenuItem;
        private ToolStripMenuItem _goToRowMenuItem;
        private ToolStripMenuItem _findMenuItem;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripButton _findStripButton;
        private Splitter _bookMarksSplitter;
        private TreeView _bookMarksTreeView;
        private ToolStripButton _bookMarksStripButton;
		private ImageList _bookMarksImageList;
    }
}
