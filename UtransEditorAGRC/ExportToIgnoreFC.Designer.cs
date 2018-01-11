namespace UtransEditorAGRC
{
    partial class ExportToIgnoreFC
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
            this.btnRun = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.cboDFC_RESULT = new System.Windows.Forms.ComboBox();
            this.cboCountyStreets = new System.Windows.Forms.ComboBox();
            this.cboUtransStreets = new System.Windows.Forms.ComboBox();
            this.cboIgnoredFC = new System.Windows.Forms.ComboBox();
            this.cboCountyName = new System.Windows.Forms.ComboBox();
            this.dateTimePickerExportIgnores = new System.Windows.Forms.DateTimePicker();
            this.SuspendLayout();
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(140, 301);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(75, 23);
            this.btnRun.TabIndex = 0;
            this.btnRun.Text = "Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(148, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Choose DFC_RESULT Layer:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(179, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Choose COUNTY_STREETS Layer:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 132);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(206, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Choose UTRANS.Roads_Edit Layer:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 185);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(146, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Choose Ignored FGDB Layer:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 239);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(103, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "County Name Layer:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(193, 240);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(108, 13);
            this.label6.TabIndex = 6;
            this.label6.Text = "Date Received Data:";
            // 
            // cboDFC_RESULT
            // 
            this.cboDFC_RESULT.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDFC_RESULT.FormattingEnabled = true;
            this.cboDFC_RESULT.Location = new System.Drawing.Point(15, 35);
            this.cboDFC_RESULT.Name = "cboDFC_RESULT";
            this.cboDFC_RESULT.Size = new System.Drawing.Size(308, 21);
            this.cboDFC_RESULT.TabIndex = 7;
            // 
            // cboCountyStreets
            // 
            this.cboCountyStreets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCountyStreets.FormattingEnabled = true;
            this.cboCountyStreets.Location = new System.Drawing.Point(15, 93);
            this.cboCountyStreets.Name = "cboCountyStreets";
            this.cboCountyStreets.Size = new System.Drawing.Size(308, 21);
            this.cboCountyStreets.TabIndex = 8;
            // 
            // cboUtransStreets
            // 
            this.cboUtransStreets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboUtransStreets.FormattingEnabled = true;
            this.cboUtransStreets.Location = new System.Drawing.Point(15, 149);
            this.cboUtransStreets.Name = "cboUtransStreets";
            this.cboUtransStreets.Size = new System.Drawing.Size(308, 21);
            this.cboUtransStreets.TabIndex = 9;
            // 
            // cboIgnoredFC
            // 
            this.cboIgnoredFC.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboIgnoredFC.FormattingEnabled = true;
            this.cboIgnoredFC.Location = new System.Drawing.Point(15, 202);
            this.cboIgnoredFC.Name = "cboIgnoredFC";
            this.cboIgnoredFC.Size = new System.Drawing.Size(308, 21);
            this.cboIgnoredFC.TabIndex = 10;
            // 
            // cboCountyName
            // 
            this.cboCountyName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCountyName.FormattingEnabled = true;
            this.cboCountyName.Items.AddRange(new object[] {
            "49001 - Beaver",
            "49003 - Box Elder",
            "49005 - Cache",
            "49007 - Carbon",
            "49009 - Daggett",
            "49011 - Davis",
            "49013 - Duchesne",
            "49015 - Emery",
            "49017 - Garfield",
            "49019 - Grand",
            "49021 - Iron",
            "49023 - Juab",
            "49025 - Kane",
            "49027 - Millard",
            "49029 - Morgan",
            "49031 - Piute",
            "49033 - Rich",
            "49035 - Salt Lake",
            "49037 - San Juan",
            "49039 - Sanpete",
            "49041 - Sevier",
            "49043 - Summit",
            "49045 - Tooele",
            "49047 - Uintah",
            "49049 - Utah",
            "49051 - Wasatch",
            "49053 - Washington",
            "49055 - Wayne",
            "49057 - Weber",
            "16031 - Cassia",
            "56041 - Uinta",
            "04015 - Mohave"});
            this.cboCountyName.Location = new System.Drawing.Point(15, 256);
            this.cboCountyName.Name = "cboCountyName";
            this.cboCountyName.Size = new System.Drawing.Size(154, 21);
            this.cboCountyName.TabIndex = 11;
            // 
            // dateTimePickerExportIgnores
            // 
            this.dateTimePickerExportIgnores.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePickerExportIgnores.Location = new System.Drawing.Point(196, 257);
            this.dateTimePickerExportIgnores.Name = "dateTimePickerExportIgnores";
            this.dateTimePickerExportIgnores.Size = new System.Drawing.Size(125, 20);
            this.dateTimePickerExportIgnores.TabIndex = 12;
            // 
            // ExportToIgnoreFC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(344, 346);
            this.Controls.Add(this.dateTimePickerExportIgnores);
            this.Controls.Add(this.cboCountyName);
            this.Controls.Add(this.cboIgnoredFC);
            this.Controls.Add(this.cboUtransStreets);
            this.Controls.Add(this.cboCountyStreets);
            this.Controls.Add(this.cboDFC_RESULT);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnRun);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportToIgnoreFC";
            this.ShowIcon = false;
            this.Text = "Export Ignored Records to Feature Class";
            this.Load += new System.EventHandler(this.ExportToIgnoreFC_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboDFC_RESULT;
        private System.Windows.Forms.ComboBox cboCountyStreets;
        private System.Windows.Forms.ComboBox cboUtransStreets;
        private System.Windows.Forms.ComboBox cboIgnoredFC;
        private System.Windows.Forms.ComboBox cboCountyName;
        private System.Windows.Forms.DateTimePicker dateTimePickerExportIgnores;
    }
}