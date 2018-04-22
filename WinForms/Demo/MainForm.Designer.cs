namespace Demo
{
    partial class MainForm
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
			this._groupButton = new System.Windows.Forms.Button();
			this._tableAndChartButton = new System.Windows.Forms.Button();
			this._barCodesButton = new System.Windows.Forms.Button();
			this._chartsButton = new System.Windows.Forms.Button();
			this._imagesButton = new System.Windows.Forms.Button();
			this._ttnButton = new System.Windows.Forms.Button();
			this._op5Button = new System.Windows.Forms.Button();
			this._kalcButton = new System.Windows.Forms.Button();
			this._pageBreakButton = new System.Windows.Forms.Button();
			this._autoHeightButton = new System.Windows.Forms.Button();
			this._autoSizeFontButton = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _groupButton
			// 
			this._groupButton.Location = new System.Drawing.Point(12, 12);
			this._groupButton.Name = "_groupButton";
			this._groupButton.Size = new System.Drawing.Size(146, 48);
			this._groupButton.TabIndex = 0;
			this._groupButton.Text = "Сгруппированный отчет";
			this._groupButton.UseVisualStyleBackColor = true;
			this._groupButton.Click += new System.EventHandler(this._groupButton_Click);
			// 
			// _tableAndChartButton
			// 
			this._tableAndChartButton.Location = new System.Drawing.Point(174, 12);
			this._tableAndChartButton.Name = "_tableAndChartButton";
			this._tableAndChartButton.Size = new System.Drawing.Size(160, 48);
			this._tableAndChartButton.TabIndex = 1;
			this._tableAndChartButton.Text = "Таблица и график";
			this._tableAndChartButton.UseVisualStyleBackColor = true;
			this._tableAndChartButton.Click += new System.EventHandler(this._tableAndChartButton_Click);
			// 
			// _barCodesButton
			// 
			this._barCodesButton.Location = new System.Drawing.Point(353, 12);
			this._barCodesButton.Name = "_barCodesButton";
			this._barCodesButton.Size = new System.Drawing.Size(151, 48);
			this._barCodesButton.TabIndex = 2;
			this._barCodesButton.Text = "Штрихкоды";
			this._barCodesButton.UseVisualStyleBackColor = true;
			this._barCodesButton.Click += new System.EventHandler(this._barCodesButton_Click);
			// 
			// _chartsButton
			// 
			this._chartsButton.Location = new System.Drawing.Point(12, 83);
			this._chartsButton.Name = "_chartsButton";
			this._chartsButton.Size = new System.Drawing.Size(146, 50);
			this._chartsButton.TabIndex = 3;
			this._chartsButton.Text = "Графики";
			this._chartsButton.UseVisualStyleBackColor = true;
			this._chartsButton.Click += new System.EventHandler(this._chartsButton_Click);
			// 
			// _imagesButton
			// 
			this._imagesButton.Location = new System.Drawing.Point(174, 83);
			this._imagesButton.Name = "_imagesButton";
			this._imagesButton.Size = new System.Drawing.Size(160, 50);
			this._imagesButton.TabIndex = 4;
			this._imagesButton.Text = "Способы вставки картинок";
			this._imagesButton.UseVisualStyleBackColor = true;
			this._imagesButton.Click += new System.EventHandler(this.button1_Click);
			// 
			// _ttnButton
			// 
			this._ttnButton.Location = new System.Drawing.Point(353, 83);
			this._ttnButton.Name = "_ttnButton";
			this._ttnButton.Size = new System.Drawing.Size(151, 50);
			this._ttnButton.TabIndex = 5;
			this._ttnButton.Text = "Товарная накладная";
			this._ttnButton.UseVisualStyleBackColor = true;
			this._ttnButton.Click += new System.EventHandler(this._ttnButton_Click);
			// 
			// _op5Button
			// 
			this._op5Button.Location = new System.Drawing.Point(12, 150);
			this._op5Button.Name = "_op5Button";
			this._op5Button.Size = new System.Drawing.Size(146, 52);
			this._op5Button.TabIndex = 6;
			this._op5Button.Text = "Закупочный акт";
			this._op5Button.UseVisualStyleBackColor = true;
			this._op5Button.Click += new System.EventHandler(this._op5Button_Click);
			// 
			// _kalcButton
			// 
			this._kalcButton.Location = new System.Drawing.Point(174, 150);
			this._kalcButton.Name = "_kalcButton";
			this._kalcButton.Size = new System.Drawing.Size(160, 52);
			this._kalcButton.TabIndex = 7;
			this._kalcButton.Text = "Калькуляционная карта";
			this._kalcButton.UseVisualStyleBackColor = true;
			this._kalcButton.Click += new System.EventHandler(this._kalcButton_Click);
			// 
			// _pageBreakButton
			// 
			this._pageBreakButton.Location = new System.Drawing.Point(353, 150);
			this._pageBreakButton.Name = "_pageBreakButton";
			this._pageBreakButton.Size = new System.Drawing.Size(151, 52);
			this._pageBreakButton.TabIndex = 8;
			this._pageBreakButton.Text = "Разрыв страницы";
			this._pageBreakButton.UseVisualStyleBackColor = true;
			this._pageBreakButton.Click += new System.EventHandler(this._pageBreakButton_Click);
			// 
			// _autoHeightButton
			// 
			this._autoHeightButton.Location = new System.Drawing.Point(12, 220);
			this._autoHeightButton.Name = "_autoHeightButton";
			this._autoHeightButton.Size = new System.Drawing.Size(146, 52);
			this._autoHeightButton.TabIndex = 9;
			this._autoHeightButton.Text = "Автовысота строк";
			this._autoHeightButton.UseVisualStyleBackColor = true;
			this._autoHeightButton.Click += new System.EventHandler(this._autoHeightButton_Click);
			// 
			// _autoSizeFontButton
			// 
			this._autoSizeFontButton.Location = new System.Drawing.Point(174, 220);
			this._autoSizeFontButton.Name = "_autoSizeFontButton";
			this._autoSizeFontButton.Size = new System.Drawing.Size(160, 52);
			this._autoSizeFontButton.TabIndex = 10;
			this._autoSizeFontButton.Text = "Автошрифт";
			this._autoSizeFontButton.UseVisualStyleBackColor = true;
			this._autoSizeFontButton.Click += new System.EventHandler(this._autoSizeFontButton_Click);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(353, 220);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(151, 52);
			this.button1.TabIndex = 11;
			this.button1.Text = "Скрытые колонки";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click_1);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(12, 290);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(146, 52);
			this.button2.TabIndex = 12;
			this.button2.Text = "Параметры";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(520, 354);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this._autoSizeFontButton);
			this.Controls.Add(this._autoHeightButton);
			this.Controls.Add(this._pageBreakButton);
			this.Controls.Add(this._kalcButton);
			this.Controls.Add(this._op5Button);
			this.Controls.Add(this._ttnButton);
			this.Controls.Add(this._imagesButton);
			this.Controls.Add(this._chartsButton);
			this.Controls.Add(this._barCodesButton);
			this.Controls.Add(this._tableAndChartButton);
			this.Controls.Add(this._groupButton);
			this.Name = "MainForm";
			this.Text = "Form1";
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button _groupButton;
        private System.Windows.Forms.Button _tableAndChartButton;
        private System.Windows.Forms.Button _barCodesButton;
        private System.Windows.Forms.Button _chartsButton;
        private System.Windows.Forms.Button _imagesButton;
        private System.Windows.Forms.Button _ttnButton;
        private System.Windows.Forms.Button _op5Button;
        private System.Windows.Forms.Button _kalcButton;
        private System.Windows.Forms.Button _pageBreakButton;
		private System.Windows.Forms.Button _autoHeightButton;
		private System.Windows.Forms.Button _autoSizeFontButton;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
	}
}

