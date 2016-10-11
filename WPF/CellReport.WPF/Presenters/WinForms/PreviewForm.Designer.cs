namespace CellReport.Presenters.WinForms
{
    partial class PreviewForm
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
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PreviewForm));
            this._previewToolStrip = new System.Windows.Forms.ToolStrip();
            this._printButton = new System.Windows.Forms.ToolStripButton();
            this._customPrintButton = new System.Windows.Forms.ToolStripButton();
            this._customPageButton = new System.Windows.Forms.ToolStripButton();
            this._autoWidthButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this._firstPageButton = new System.Windows.Forms.ToolStripButton();
            this._prevPageButton = new System.Windows.Forms.ToolStripButton();
            this._currentPageTextBox = new System.Windows.Forms.ToolStripTextBox();
            this._nextPageButton = new System.Windows.Forms.ToolStripButton();
            this._lastPageButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this._previewControl = new System.Windows.Forms.PrintPreviewControl();
            this._previewToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // _previewToolStrip
            // 
            this._previewToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this._previewToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._printButton,
            this._customPrintButton,
            this._customPageButton,
            this._autoWidthButton,
            this.toolStripSeparator1,
            this._firstPageButton,
            this._prevPageButton,
            this._currentPageTextBox,
            this._nextPageButton,
            this._lastPageButton,
            this.toolStripSeparator2});
            this._previewToolStrip.Location = new System.Drawing.Point(0, 0);
            this._previewToolStrip.Name = "_previewToolStrip";
            this._previewToolStrip.Size = new System.Drawing.Size(510, 25);
            this._previewToolStrip.TabIndex = 0;
            // 
            // _printButton
            // 
            this._printButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._printButton.Image = global::CellReport.Properties.Resources.printer;
            this._printButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._printButton.Name = "_printButton";
            this._printButton.Size = new System.Drawing.Size(23, 22);
            this._printButton.Text = "toolStripButton1";
            this._printButton.ToolTipText = "Быстрая печать";
            this._printButton.Click += new System.EventHandler(this._printButton_Click);
            // 
            // _customPrintButton
            // 
            this._customPrintButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._customPrintButton.Image = global::CellReport.Properties.Resources.printer_custom;
            this._customPrintButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._customPrintButton.Name = "_customPrintButton";
            this._customPrintButton.Size = new System.Drawing.Size(23, 22);
            this._customPrintButton.Text = "toolStripButton2";
            this._customPrintButton.ToolTipText = "Выборочная печать";
            this._customPrintButton.Click += new System.EventHandler(this._customPrintButton_Click);
            // 
            // _customPageButton
            // 
            this._customPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._customPageButton.Image = global::CellReport.Properties.Resources.document__pencil;
            this._customPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._customPageButton.Name = "_customPageButton";
            this._customPageButton.Size = new System.Drawing.Size(23, 22);
            this._customPageButton.Text = "toolStripButton3";
            this._customPageButton.ToolTipText = "Параметры страницы";
            this._customPageButton.Click += new System.EventHandler(this._customPageButton_Click);
            // 
            // _autoWidthButton
            // 
            this._autoWidthButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._autoWidthButton.Image = global::CellReport.Properties.Resources.AutoWidth;
            this._autoWidthButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._autoWidthButton.Name = "_autoWidthButton";
            this._autoWidthButton.Size = new System.Drawing.Size(23, 22);
            this._autoWidthButton.ToolTipText = "Автоширина по странице";
            this._autoWidthButton.Click += new System.EventHandler(this._autoWidthButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // _firstPageButton
            // 
            this._firstPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._firstPageButton.Image = global::CellReport.Properties.Resources.FirstPage;
            this._firstPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._firstPageButton.Name = "_firstPageButton";
            this._firstPageButton.Size = new System.Drawing.Size(23, 22);
            this._firstPageButton.ToolTipText = "Первая страница";
            this._firstPageButton.Click += new System.EventHandler(this._firstPageButton_Click);
            // 
            // _prevPageButton
            // 
            this._prevPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._prevPageButton.Image = global::CellReport.Properties.Resources.PrevPage;
            this._prevPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._prevPageButton.Name = "_prevPageButton";
            this._prevPageButton.Size = new System.Drawing.Size(23, 22);
            this._prevPageButton.Text = "toolStripButton2";
            this._prevPageButton.ToolTipText = "Предыдущая страница";
            this._prevPageButton.Click += new System.EventHandler(this._prevPageButton_Click);
            // 
            // _currentPageTextBox
            // 
            this._currentPageTextBox.Enabled = false;
            this._currentPageTextBox.MaxLength = 3;
            this._currentPageTextBox.Name = "_currentPageTextBox";
            this._currentPageTextBox.Size = new System.Drawing.Size(30, 25);
            this._currentPageTextBox.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // _nextPageButton
            // 
            this._nextPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._nextPageButton.Image = global::CellReport.Properties.Resources.NextPage;
            this._nextPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._nextPageButton.Name = "_nextPageButton";
            this._nextPageButton.Size = new System.Drawing.Size(23, 22);
            this._nextPageButton.Text = "toolStripButton3";
            this._nextPageButton.ToolTipText = "Следующая страница";
            this._nextPageButton.Click += new System.EventHandler(this._nextPageButton_Click);
            // 
            // _lastPageButton
            // 
            this._lastPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this._lastPageButton.Image = global::CellReport.Properties.Resources.LastPage;
            this._lastPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this._lastPageButton.Name = "_lastPageButton";
            this._lastPageButton.Size = new System.Drawing.Size(23, 22);
            this._lastPageButton.Text = "toolStripButton4";
            this._lastPageButton.ToolTipText = "Последняя страница";
            this._lastPageButton.Click += new System.EventHandler(this._lastPageButton_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // _previewControl
            // 
            this._previewControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._previewControl.Location = new System.Drawing.Point(0, 25);
            this._previewControl.Name = "_previewControl";
            this._previewControl.Size = new System.Drawing.Size(510, 411);
            this._previewControl.TabIndex = 5;
            // 
            // PreviewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(510, 436);
            this.Controls.Add(this._previewControl);
            this.Controls.Add(this._previewToolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "PreviewForm";
            this.ShowInTaskbar = false;
            this.Text = "Предварительный просмотр";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this._previewToolStrip.ResumeLayout(false);
            this._previewToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip _previewToolStrip;
        internal System.Windows.Forms.PrintPreviewControl _previewControl;
        private System.Windows.Forms.ToolStripButton _printButton;
        private System.Windows.Forms.ToolStripButton _customPrintButton;
        private System.Windows.Forms.ToolStripButton _customPageButton;
        private System.Windows.Forms.ToolStripButton _autoWidthButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton _firstPageButton;
        private System.Windows.Forms.ToolStripButton _prevPageButton;
        private System.Windows.Forms.ToolStripTextBox _currentPageTextBox;
        private System.Windows.Forms.ToolStripButton _nextPageButton;
        private System.Windows.Forms.ToolStripButton _lastPageButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
    }
}