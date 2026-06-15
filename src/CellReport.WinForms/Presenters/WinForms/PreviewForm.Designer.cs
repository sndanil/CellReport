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
            _previewToolStrip = new System.Windows.Forms.ToolStrip();
            _printButton = new System.Windows.Forms.ToolStripButton();
            _customPageButton = new System.Windows.Forms.ToolStripButton();
            _autoWidthButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            _firstPageButton = new System.Windows.Forms.ToolStripButton();
            _prevPageButton = new System.Windows.Forms.ToolStripButton();
            _currentPageTextBox = new System.Windows.Forms.ToolStripTextBox();
            _nextPageButton = new System.Windows.Forms.ToolStripButton();
            _lastPageButton = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            _previewControl = new System.Windows.Forms.PrintPreviewControl();
            _previewToolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // _previewToolStrip
            // 
            _previewToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _previewToolStrip.ImageScalingSize = new System.Drawing.Size(28, 28);
            _previewToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _printButton, _customPageButton, _autoWidthButton, toolStripSeparator1, _firstPageButton, _prevPageButton, _currentPageTextBox, _nextPageButton, _lastPageButton, toolStripSeparator2 });
            _previewToolStrip.Location = new System.Drawing.Point(0, 0);
            _previewToolStrip.Name = "_previewToolStrip";
            _previewToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 4, 0);
            _previewToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            _previewToolStrip.Size = new System.Drawing.Size(1020, 40);
            _previewToolStrip.TabIndex = 0;
            // 
            // _printButton
            // 
            _printButton.Image = CellReport.WinForms.ToolbarIcons.Print;
            _printButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _printButton.Name = "_printButton";
            _printButton.Size = new System.Drawing.Size(114, 34);
            _printButton.Text = "Печать";
            _printButton.ToolTipText = "Быстрая печать";
            _printButton.Click += _printButton_Click;
            // 
            // _customPageButton
            // 
            _customPageButton.Image = CellReport.WinForms.ToolbarIcons.PrintSetup;
            _customPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _customPageButton.Name = "_customPageButton";
            _customPageButton.Size = new System.Drawing.Size(256, 34);
            _customPageButton.Text = "Параметры страницы";
            _customPageButton.ToolTipText = "Параметры страницы";
            _customPageButton.Click += _customPageButton_Click;
            // 
            // _autoWidthButton
            // 
            _autoWidthButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _autoWidthButton.Image = CellReport.WinForms.ToolbarIcons.DocumentArrowLeftright;
            _autoWidthButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _autoWidthButton.Name = "_autoWidthButton";
            _autoWidthButton.Size = new System.Drawing.Size(40, 34);
            _autoWidthButton.ToolTipText = "Автоширина по странице";
            _autoWidthButton.Click += _autoWidthButton_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 40);
            // 
            // _firstPageButton
            // 
            _firstPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _firstPageButton.Image = CellReport.WinForms.ToolbarIcons.NavBlueLeft;
            _firstPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _firstPageButton.Name = "_firstPageButton";
            _firstPageButton.Size = new System.Drawing.Size(40, 34);
            _firstPageButton.ToolTipText = "Первая страница";
            _firstPageButton.Click += _firstPageButton_Click;
            // 
            // _prevPageButton
            // 
            _prevPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _prevPageButton.Image = CellReport.WinForms.ToolbarIcons.ArrowLeft;
            _prevPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _prevPageButton.Name = "_prevPageButton";
            _prevPageButton.Size = new System.Drawing.Size(40, 34);
            _prevPageButton.Text = "toolStripButton2";
            _prevPageButton.ToolTipText = "Предыдущая страница";
            _prevPageButton.Click += _prevPageButton_Click;
            // 
            // _currentPageTextBox
            // 
            _currentPageTextBox.Enabled = false;
            _currentPageTextBox.MaxLength = 3;
            _currentPageTextBox.Name = "_currentPageTextBox";
            _currentPageTextBox.Size = new System.Drawing.Size(30, 40);
            _currentPageTextBox.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // _nextPageButton
            // 
            _nextPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _nextPageButton.Image = CellReport.WinForms.ToolbarIcons.ArrowRight;
            _nextPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _nextPageButton.Name = "_nextPageButton";
            _nextPageButton.Size = new System.Drawing.Size(40, 34);
            _nextPageButton.Text = "toolStripButton3";
            _nextPageButton.ToolTipText = "Следующая страница";
            _nextPageButton.Click += _nextPageButton_Click;
            // 
            // _lastPageButton
            // 
            _lastPageButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            _lastPageButton.Image = CellReport.WinForms.ToolbarIcons.NavBlueRight;
            _lastPageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            _lastPageButton.Name = "_lastPageButton";
            _lastPageButton.Size = new System.Drawing.Size(40, 34);
            _lastPageButton.Text = "toolStripButton4";
            _lastPageButton.ToolTipText = "Последняя страница";
            _lastPageButton.Click += _lastPageButton_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(6, 40);
            // 
            // _previewControl
            // 
            _previewControl.Columns = 3;
            _previewControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _previewControl.Location = new System.Drawing.Point(0, 40);
            _previewControl.Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            _previewControl.Name = "_previewControl";
            _previewControl.Rows = 3;
            _previewControl.Size = new System.Drawing.Size(1020, 966);
            _previewControl.TabIndex = 5;
            // 
            // PreviewForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(12F, 30F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1020, 1006);
            Controls.Add(_previewControl);
            Controls.Add(_previewToolStrip);
            Margin = new System.Windows.Forms.Padding(6, 7, 6, 7);
            Name = "PreviewForm";
            Icon = CellReport.WinForms.ToolbarIcons.App;
            ShowInTaskbar = false;
            Text = "Предварительный просмотр";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            _previewToolStrip.ResumeLayout(false);
            _previewToolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip _previewToolStrip;
        internal System.Windows.Forms.PrintPreviewControl _previewControl;
        private System.Windows.Forms.ToolStripButton _printButton;
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