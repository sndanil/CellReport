namespace CellReport.WinForms.Presenters.WinForms
{
    partial class FindForm
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
			this.label1 = new System.Windows.Forms.Label();
			this._findComboBox = new System.Windows.Forms.ComboBox();
			this._caseSensCheckBox = new System.Windows.Forms.CheckBox();
			this._matchWordOnlyCheckBox = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this._directionComboBox = new System.Windows.Forms.ComboBox();
			this._findByComboBox = new System.Windows.Forms.ComboBox();
			this._findAllButton = new System.Windows.Forms.Button();
			this._findNextButton = new System.Windows.Forms.Button();
			this._cancelButton = new System.Windows.Forms.Button();
			this._findListView = new System.Windows.Forms.ListView();
			this._contentHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this._rowHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this._columnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 18);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(136, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "#Report/Search/FindWhat";
			// 
			// _findComboBox
			// 
			this._findComboBox.FormattingEnabled = true;
			this._findComboBox.Location = new System.Drawing.Point(120, 15);
			this._findComboBox.Name = "_findComboBox";
			this._findComboBox.Size = new System.Drawing.Size(329, 21);
			this._findComboBox.TabIndex = 0;
			// 
			// _caseSensCheckBox
			// 
			this._caseSensCheckBox.AutoSize = true;
			this._caseSensCheckBox.Location = new System.Drawing.Point(281, 58);
			this._caseSensCheckBox.Name = "_caseSensCheckBox";
			this._caseSensCheckBox.Size = new System.Drawing.Size(163, 17);
			this._caseSensCheckBox.TabIndex = 3;
			this._caseSensCheckBox.Text = "#Report/Search/MatchCase";
			this._caseSensCheckBox.UseVisualStyleBackColor = true;
			// 
			// _matchWordOnlyCheckBox
			// 
			this._matchWordOnlyCheckBox.AutoSize = true;
			this._matchWordOnlyCheckBox.Location = new System.Drawing.Point(281, 87);
			this._matchWordOnlyCheckBox.Name = "_matchWordOnlyCheckBox";
			this._matchWordOnlyCheckBox.Size = new System.Drawing.Size(196, 17);
			this._matchWordOnlyCheckBox.TabIndex = 4;
			this._matchWordOnlyCheckBox.Text = "#Report/Search/MatchWholeWord";
			this._matchWordOnlyCheckBox.UseVisualStyleBackColor = true;
			this._matchWordOnlyCheckBox.Visible = false;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 59);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(132, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "#Report/Search/Direction";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 86);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(137, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "#Report/Search/LookOver";
			// 
			// _directionComboBox
			// 
			this._directionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._directionComboBox.FormattingEnabled = true;
			this._directionComboBox.Location = new System.Drawing.Point(120, 56);
			this._directionComboBox.Name = "_directionComboBox";
			this._directionComboBox.Size = new System.Drawing.Size(121, 21);
			this._directionComboBox.TabIndex = 1;
			// 
			// _findByComboBox
			// 
			this._findByComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this._findByComboBox.FormattingEnabled = true;
			this._findByComboBox.Location = new System.Drawing.Point(120, 83);
			this._findByComboBox.Name = "_findByComboBox";
			this._findByComboBox.Size = new System.Drawing.Size(121, 21);
			this._findByComboBox.TabIndex = 2;
			// 
			// _findAllButton
			// 
			this._findAllButton.Location = new System.Drawing.Point(148, 140);
			this._findAllButton.Name = "_findAllButton";
			this._findAllButton.Size = new System.Drawing.Size(100, 30);
			this._findAllButton.TabIndex = 5;
			this._findAllButton.Text = "#Report/Search/All";
			this._findAllButton.UseVisualStyleBackColor = true;
			this._findAllButton.Click += new System.EventHandler(this.FindAllButtonClick);
			// 
			// _findNextButton
			// 
			this._findNextButton.Location = new System.Drawing.Point(254, 140);
			this._findNextButton.Name = "_findNextButton";
			this._findNextButton.Size = new System.Drawing.Size(102, 30);
			this._findNextButton.TabIndex = 6;
			this._findNextButton.Text = "#Report/Search/Next";
			this._findNextButton.UseVisualStyleBackColor = true;
			this._findNextButton.Click += new System.EventHandler(this.FindNextButtonClick);
			// 
			// _cancelButton
			// 
			this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._cancelButton.Location = new System.Drawing.Point(374, 140);
			this._cancelButton.Name = "_cancelButton";
			this._cancelButton.Size = new System.Drawing.Size(75, 30);
			this._cancelButton.TabIndex = 7;
			this._cancelButton.Text = "#Report/Search/Close";
			this._cancelButton.UseVisualStyleBackColor = true;
			this._cancelButton.Click += new System.EventHandler(this.CancelButtonClick);
			// 
			// _findListView
			// 
			this._findListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._contentHeader,
            this._rowHeader,
            this._columnHeader});
			this._findListView.Location = new System.Drawing.Point(12, 183);
			this._findListView.Name = "_findListView";
			this._findListView.Size = new System.Drawing.Size(437, 125);
			this._findListView.TabIndex = 8;
			this._findListView.UseCompatibleStateImageBehavior = false;
			this._findListView.View = System.Windows.Forms.View.Details;
			this._findListView.SelectedIndexChanged += new System.EventHandler(this.FindListViewSelectedIndexChanged);
			// 
			// _contentHeader
			// 
			this._contentHeader.Text = "#Report/Search/Result/Content";
			this._contentHeader.Width = 290;
			// 
			// _rowHeader
			// 
			this._rowHeader.Text = "#Report/Search/Result/Row";
			this._rowHeader.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// _columnHeader
			// 
			this._columnHeader.Text = "#Report/Search/Result/Column";
			this._columnHeader.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// FindForm
			// 
			this.AcceptButton = this._findNextButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._cancelButton;
			this.ClientSize = new System.Drawing.Size(461, 320);
			this.Controls.Add(this._findListView);
			this.Controls.Add(this._cancelButton);
			this.Controls.Add(this._findNextButton);
			this.Controls.Add(this._findAllButton);
			this.Controls.Add(this._findByComboBox);
			this.Controls.Add(this._directionComboBox);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this._matchWordOnlyCheckBox);
			this.Controls.Add(this._caseSensCheckBox);
			this.Controls.Add(this._findComboBox);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FindForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "#Report/Search/Title";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox _findComboBox;
        private System.Windows.Forms.CheckBox _caseSensCheckBox;
        private System.Windows.Forms.CheckBox _matchWordOnlyCheckBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox _directionComboBox;
        private System.Windows.Forms.ComboBox _findByComboBox;
        private System.Windows.Forms.Button _findAllButton;
        private System.Windows.Forms.Button _findNextButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.ListView _findListView;
        private System.Windows.Forms.ColumnHeader _contentHeader;
        private System.Windows.Forms.ColumnHeader _rowHeader;
        private System.Windows.Forms.ColumnHeader _columnHeader;
    }
}