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
			this.components = new System.ComponentModel.Container();
			this._vScrollBar = new System.Windows.Forms.VScrollBar();
			this._hScrollBar = new System.Windows.Forms.HScrollBar();
			this._gridPanel = new System.Windows.Forms.Panel();
			this._contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this._printCntxMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._customPrintCntxMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._previewCntxMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._customPageCntxMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._autoWidthCntxMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this._showGridMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._showResizersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._goToRowMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._findMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._exportExcelMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._bookMarksSplitter = new System.Windows.Forms.Splitter();
			this._bookMarksTreeView = new System.Windows.Forms.TreeView();
			this._bookMarksImageList = new System.Windows.Forms.ImageList(this.components);
			this._toolBar = new System.Windows.Forms.ToolStrip();
			this._printButton = new System.Windows.Forms.ToolStripSplitButton();
			this._customPrintMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._printPreviewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._pageSetupMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._autoWidthMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this._showGridButton = new System.Windows.Forms.ToolStripButton();
			this._showResizersButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this._scaleComboBox = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this._excelExportButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this._findStripButton = new System.Windows.Forms.ToolStripButton();
			this._bookMarksStripButton = new System.Windows.Forms.ToolStripButton();
			this._paintPanel = new CellReport.Presenters.WinForms.DoubleBufferedControl();
			this._columnResizer = new CellReport.Presenters.WinForms.ColumnResizerControl();
			this._rowsResizer = new CellReport.Presenters.WinForms.RowResizerControl();
			this._gridPanel.SuspendLayout();
			this._contextMenu.SuspendLayout();
			this._toolBar.SuspendLayout();
			this.SuspendLayout();
			// 
			// _vScrollBar
			// 
			this._vScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._vScrollBar.LargeChange = 5;
			this._vScrollBar.Location = new System.Drawing.Point(559, 0);
			this._vScrollBar.Name = "_vScrollBar";
			this._vScrollBar.Size = new System.Drawing.Size(17, 457);
			this._vScrollBar.TabIndex = 0;
			this._vScrollBar.ValueChanged += new System.EventHandler(this.ScrollBar_ValueChanged);
			// 
			// _hScrollBar
			// 
			this._hScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._hScrollBar.LargeChange = 5;
			this._hScrollBar.Location = new System.Drawing.Point(0, 457);
			this._hScrollBar.Name = "_hScrollBar";
			this._hScrollBar.Size = new System.Drawing.Size(559, 17);
			this._hScrollBar.TabIndex = 1;
			this._hScrollBar.ValueChanged += new System.EventHandler(this.ScrollBar_ValueChanged);
			// 
			// _gridPanel
			// 
			this._gridPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._gridPanel.Controls.Add(this._paintPanel);
			this._gridPanel.Controls.Add(this._columnResizer);
			this._gridPanel.Controls.Add(this._rowsResizer);
			this._gridPanel.Controls.Add(this._bookMarksSplitter);
			this._gridPanel.Controls.Add(this._bookMarksTreeView);
			this._gridPanel.Controls.Add(this._toolBar);
			this._gridPanel.Location = new System.Drawing.Point(0, 0);
			this._gridPanel.Name = "_gridPanel";
			this._gridPanel.Size = new System.Drawing.Size(559, 457);
			this._gridPanel.TabIndex = 3;
			// 
			// _contextMenu
			// 
			this._contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._printCntxMenuItem,
            this.toolStripSeparator2,
            this._showGridMenuItem,
            this._showResizersMenuItem,
            this._goToRowMenuItem,
            this._findMenuItem,
            this._exportExcelMenuItem,
            this._saveMenuItem});
			this._contextMenu.Name = "_contextMenu";
			this._contextMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._contextMenu.Size = new System.Drawing.Size(219, 164);
			// 
			// _printCntxMenuItem
			// 
			this._printCntxMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._customPrintCntxMenuItem,
            this._previewCntxMenuItem,
            this._customPageCntxMenuItem,
            this._autoWidthCntxMenuItem});
			this._printCntxMenuItem.Image = global::CellReport.WinForms.Properties.Resources.print;
			this._printCntxMenuItem.Name = "_printCntxMenuItem";
			this._printCntxMenuItem.Size = new System.Drawing.Size(218, 22);
			this._printCntxMenuItem.Text = "#Report/Print";
			this._printCntxMenuItem.Click += new System.EventHandler(this.PrintButtonClick);
			// 
			// _customPrintCntxMenuItem
			// 
			this._customPrintCntxMenuItem.Image = global::CellReport.WinForms.Properties.Resources.print;
			this._customPrintCntxMenuItem.Name = "_customPrintCntxMenuItem";
			this._customPrintCntxMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this._customPrintCntxMenuItem.Size = new System.Drawing.Size(229, 22);
			this._customPrintCntxMenuItem.Text = "#Report/CustomPrint";
			this._customPrintCntxMenuItem.Click += new System.EventHandler(this.CustomPrintMenuItemClick);
			// 
			// _previewCntxMenuItem
			// 
			this._previewCntxMenuItem.Image = global::CellReport.WinForms.Properties.Resources.print_preview;
			this._previewCntxMenuItem.Name = "_previewCntxMenuItem";
			this._previewCntxMenuItem.Size = new System.Drawing.Size(229, 22);
			this._previewCntxMenuItem.Text = "#Report/Preview";
			this._previewCntxMenuItem.Click += new System.EventHandler(this.PrintPreviewMenuItemClick);
			// 
			// _customPageCntxMenuItem
			// 
			this._customPageCntxMenuItem.Image = global::CellReport.WinForms.Properties.Resources.print_setup;
			this._customPageCntxMenuItem.Name = "_customPageCntxMenuItem";
			this._customPageCntxMenuItem.Size = new System.Drawing.Size(229, 22);
			this._customPageCntxMenuItem.Text = "#Report/PageSettings";
			this._customPageCntxMenuItem.Click += new System.EventHandler(this.PageSetupMenuItemClick);
			// 
			// _autoWidthCntxMenuItem
			// 
			this._autoWidthCntxMenuItem.Name = "_autoWidthCntxMenuItem";
			this._autoWidthCntxMenuItem.Size = new System.Drawing.Size(229, 22);
			this._autoWidthCntxMenuItem.Text = "#Report/AutoFitPage";
			this._autoWidthCntxMenuItem.Click += new System.EventHandler(this.AutoWidthMenuItemClick);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(215, 6);
			// 
			// _showGridMenuItem
			// 
			this._showGridMenuItem.Image = global::CellReport.WinForms.Properties.Resources.show_grid_16x16;
			this._showGridMenuItem.Name = "_showGridMenuItem";
			this._showGridMenuItem.Size = new System.Drawing.Size(218, 22);
			this._showGridMenuItem.Text = "#Report/ShowHideGrid";
			this._showGridMenuItem.Click += new System.EventHandler(this.ShowGridButtonClick);
			// 
			// _showResizersMenuItem
			// 
			this._showResizersMenuItem.Image = global::CellReport.WinForms.Properties.Resources.show_columns_16x16;
			this._showResizersMenuItem.Name = "_showResizersMenuItem";
			this._showResizersMenuItem.Size = new System.Drawing.Size(218, 22);
			this._showResizersMenuItem.Text = "#Report/ShowHideHeaders";
			this._showResizersMenuItem.Click += new System.EventHandler(this.ShowResizersButtonClick);
			// 
			// _goToRowMenuItem
			// 
			this._goToRowMenuItem.Name = "_goToRowMenuItem";
			this._goToRowMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
			this._goToRowMenuItem.Size = new System.Drawing.Size(218, 22);
			this._goToRowMenuItem.Text = "#Report/GoToLine";
			this._goToRowMenuItem.Click += new System.EventHandler(this.GoToRowMenuItemClick);
			// 
			// _findMenuItem
			// 
			this._findMenuItem.Image = global::CellReport.WinForms.Properties.Resources.find;
			this._findMenuItem.Name = "_findMenuItem";
			this._findMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
			this._findMenuItem.Size = new System.Drawing.Size(218, 22);
			this._findMenuItem.Text = "#Report/Search";
			this._findMenuItem.Click += new System.EventHandler(this.FindMenuItemClick);
			// 
			// _exportExcelMenuItem
			// 
			this._exportExcelMenuItem.Image = global::CellReport.WinForms.Properties.Resources.excel2013_16;
			this._exportExcelMenuItem.Name = "_exportExcelMenuItem";
			this._exportExcelMenuItem.Size = new System.Drawing.Size(218, 22);
			this._exportExcelMenuItem.Text = "#Report/ExportToExcel";
			this._exportExcelMenuItem.Click += new System.EventHandler(this.ExportExcelButtonClick);
			// 
			// _saveMenuItem
			// 
			this._saveMenuItem.Image = global::CellReport.WinForms.Properties.Resources.save;
			this._saveMenuItem.Name = "_saveMenuItem";
			this._saveMenuItem.Size = new System.Drawing.Size(218, 22);
			this._saveMenuItem.Text = "#Report/Save";
			this._saveMenuItem.Click += new System.EventHandler(this.SaveMenuItemClick);
			// 
			// _bookMarksSplitter
			// 
			this._bookMarksSplitter.Location = new System.Drawing.Point(170, 25);
			this._bookMarksSplitter.Name = "_bookMarksSplitter";
			this._bookMarksSplitter.Size = new System.Drawing.Size(3, 432);
			this._bookMarksSplitter.TabIndex = 7;
			this._bookMarksSplitter.TabStop = false;
			this._bookMarksSplitter.Visible = false;
			// 
			// _bookMarksTreeView
			// 
			this._bookMarksTreeView.Dock = System.Windows.Forms.DockStyle.Left;
			this._bookMarksTreeView.ImageIndex = 0;
			this._bookMarksTreeView.ImageList = this._bookMarksImageList;
			this._bookMarksTreeView.Location = new System.Drawing.Point(0, 25);
			this._bookMarksTreeView.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
			this._bookMarksTreeView.Name = "_bookMarksTreeView";
			this._bookMarksTreeView.SelectedImageIndex = 0;
			this._bookMarksTreeView.Size = new System.Drawing.Size(170, 432);
			this._bookMarksTreeView.TabIndex = 6;
			this._bookMarksTreeView.Visible = false;
			this._bookMarksTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this._bookMarksTreeView_AfterSelect);
			// 
			// _bookMarksImageList
			// 
			this._bookMarksImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this._bookMarksImageList.ImageSize = new System.Drawing.Size(16, 16);
			this._bookMarksImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// _toolBar
			// 
			this._toolBar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this._toolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._printButton,
            this.toolStripSeparator1,
            this._showGridButton,
            this._showResizersButton,
            this.toolStripSeparator3,
            this._scaleComboBox,
            this.toolStripSeparator4,
            this._excelExportButton,
            this.toolStripSeparator5,
            this._findStripButton,
            this._bookMarksStripButton});
			this._toolBar.Location = new System.Drawing.Point(0, 0);
			this._toolBar.Name = "_toolBar";
			this._toolBar.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this._toolBar.Size = new System.Drawing.Size(559, 25);
			this._toolBar.TabIndex = 5;
			// 
			// _printButton
			// 
			this._printButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._customPrintMenuItem,
            this._printPreviewMenuItem,
            this._pageSetupMenuItem,
            this._autoWidthMenuItem});
			this._printButton.Image = global::CellReport.WinForms.Properties.Resources.print;
			this._printButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._printButton.Name = "_printButton";
			this._printButton.Size = new System.Drawing.Size(111, 22);
			this._printButton.Text = "#Report/Print";
			this._printButton.ButtonClick += new System.EventHandler(this.PrintButtonClick);
			// 
			// _customPrintMenuItem
			// 
			this._customPrintMenuItem.Image = global::CellReport.WinForms.Properties.Resources.print;
			this._customPrintMenuItem.Name = "_customPrintMenuItem";
			this._customPrintMenuItem.Size = new System.Drawing.Size(189, 22);
			this._customPrintMenuItem.Text = "#Report/CustomPrint";
			this._customPrintMenuItem.Click += new System.EventHandler(this.CustomPrintMenuItemClick);
			// 
			// _printPreviewMenuItem
			// 
			this._printPreviewMenuItem.Image = global::CellReport.WinForms.Properties.Resources.print_preview;
			this._printPreviewMenuItem.Name = "_printPreviewMenuItem";
			this._printPreviewMenuItem.Size = new System.Drawing.Size(189, 22);
			this._printPreviewMenuItem.Text = "#Report/Preview";
			this._printPreviewMenuItem.Click += new System.EventHandler(this.PrintPreviewMenuItemClick);
			// 
			// _pageSetupMenuItem
			// 
			this._pageSetupMenuItem.Image = global::CellReport.WinForms.Properties.Resources.print_setup;
			this._pageSetupMenuItem.Name = "_pageSetupMenuItem";
			this._pageSetupMenuItem.Size = new System.Drawing.Size(189, 22);
			this._pageSetupMenuItem.Text = "#Report/PageSettings";
			this._pageSetupMenuItem.Click += new System.EventHandler(this.PageSetupMenuItemClick);
			// 
			// _autoWidthMenuItem
			// 
			this._autoWidthMenuItem.Name = "_autoWidthMenuItem";
			this._autoWidthMenuItem.Size = new System.Drawing.Size(189, 22);
			this._autoWidthMenuItem.Text = "#Report/AutoFitPage";
			this._autoWidthMenuItem.Click += new System.EventHandler(this.AutoWidthMenuItemClick);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// _showGridButton
			// 
			this._showGridButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._showGridButton.Image = global::CellReport.WinForms.Properties.Resources.show_grid_16x16;
			this._showGridButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._showGridButton.Name = "_showGridButton";
			this._showGridButton.Size = new System.Drawing.Size(23, 22);
			this._showGridButton.Text = "#Report/ShowHideGrid";
			this._showGridButton.Click += new System.EventHandler(this.ShowGridButtonClick);
			// 
			// _showResizersButton
			// 
			this._showResizersButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._showResizersButton.Image = global::CellReport.WinForms.Properties.Resources.show_columns_16x16;
			this._showResizersButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._showResizersButton.Name = "_showResizersButton";
			this._showResizersButton.Size = new System.Drawing.Size(23, 22);
			this._showResizersButton.Text = "#Report/ShowHideHeaders";
			this._showResizersButton.Click += new System.EventHandler(this.ShowResizersButtonClick);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// _scaleComboBox
			// 
			this._scaleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._scaleComboBox.Items.AddRange(new object[] {
            "50%",
            "75%",
            "85%",
            "100%",
            "125%",
            "150%",
            "175%",
            "200%",
            "300%",
            "500%"});
			this._scaleComboBox.Name = "_scaleComboBox";
			this._scaleComboBox.Size = new System.Drawing.Size(75, 25);
			this._scaleComboBox.SelectedIndexChanged += new System.EventHandler(this.ScaleComboBoxSelectedIndexChanged);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
			// 
			// _excelExportButton
			// 
			this._excelExportButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._excelExportButton.Image = global::CellReport.WinForms.Properties.Resources.excel2013_16;
			this._excelExportButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._excelExportButton.Name = "_excelExportButton";
			this._excelExportButton.Size = new System.Drawing.Size(23, 22);
			this._excelExportButton.Text = "#Report/ExportToExcel";
			this._excelExportButton.Click += new System.EventHandler(this.ExportExcelButtonClick);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(6, 25);
			// 
			// _findStripButton
			// 
			this._findStripButton.Image = global::CellReport.WinForms.Properties.Resources.find;
			this._findStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._findStripButton.Name = "_findStripButton";
			this._findStripButton.Size = new System.Drawing.Size(109, 22);
			this._findStripButton.Text = "#Report/Search";
			this._findStripButton.Click += new System.EventHandler(this.FindMenuItemClick);
			// 
			// _bookMarksStripButton
			// 
			this._bookMarksStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._bookMarksStripButton.Image = global::CellReport.WinForms.Properties.Resources.bookmark;
			this._bookMarksStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._bookMarksStripButton.Name = "_bookMarksStripButton";
			this._bookMarksStripButton.Size = new System.Drawing.Size(23, 22);
			this._bookMarksStripButton.Text = "#Report/BookMarks";
			this._bookMarksStripButton.Click += new System.EventHandler(this.BookMarksStripButtonClick);
			// 
			// _paintPanel
			// 
			this._paintPanel.BackColor = System.Drawing.Color.White;
			this._paintPanel.ContextMenuStrip = this._contextMenu;
			this._paintPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._paintPanel.Location = new System.Drawing.Point(213, 43);
			this._paintPanel.Name = "_paintPanel";
			this._paintPanel.Size = new System.Drawing.Size(346, 414);
			this._paintPanel.TabIndex = 2;
			// 
			// _columnResizer
			// 
			this._columnResizer.BackColor = System.Drawing.SystemColors.ActiveBorder;
			this._columnResizer.Dock = System.Windows.Forms.DockStyle.Top;
			this._columnResizer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(65)))), ((int)(((byte)(62)))));
			this._columnResizer.Location = new System.Drawing.Point(213, 25);
			this._columnResizer.Name = "_columnResizer";
			this._columnResizer.Presenter = null;
			this._columnResizer.Size = new System.Drawing.Size(346, 18);
			this._columnResizer.TabIndex = 3;
			this._columnResizer.TabStop = false;
			// 
			// _rowsResizer
			// 
			this._rowsResizer.BackColor = System.Drawing.SystemColors.ActiveBorder;
			this._rowsResizer.Dock = System.Windows.Forms.DockStyle.Left;
			this._rowsResizer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(65)))), ((int)(((byte)(62)))));
			this._rowsResizer.Location = new System.Drawing.Point(173, 25);
			this._rowsResizer.Name = "_rowsResizer";
			this._rowsResizer.Presenter = null;
			this._rowsResizer.Size = new System.Drawing.Size(40, 432);
			this._rowsResizer.TabIndex = 4;
			this._rowsResizer.TabStop = false;
			this._rowsResizer.TopOffset = 0;
			// 
			// PresentControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._gridPanel);
			this.Controls.Add(this._hScrollBar);
			this.Controls.Add(this._vScrollBar);
			this.Name = "PresentControl";
			this.Size = new System.Drawing.Size(576, 474);
			this._gridPanel.ResumeLayout(false);
			this._gridPanel.PerformLayout();
			this._contextMenu.ResumeLayout(false);
			this._toolBar.ResumeLayout(false);
			this._toolBar.PerformLayout();
			this.ResumeLayout(false);

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
        private ToolStripMenuItem _exportExcelMenuItem;
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
