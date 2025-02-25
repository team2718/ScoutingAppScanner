namespace CameraFeedApp
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.TextScannedText = new System.Windows.Forms.Label();
            this.TextScanned = new System.Windows.Forms.Label();
            this.SaveToText = new System.Windows.Forms.Button();
            this.TextScanned2 = new System.Windows.Forms.Label();
            this.SaveToCSV = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(802, 451);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // TextScannedText
            // 
            this.TextScannedText.AutoSize = true;
            this.TextScannedText.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextScannedText.ForeColor = System.Drawing.SystemColors.ControlText;
            this.TextScannedText.Location = new System.Drawing.Point(8, 8);
            this.TextScannedText.Name = "TextScannedText";
            this.TextScannedText.Size = new System.Drawing.Size(321, 55);
            this.TextScannedText.TabIndex = 1;
            this.TextScannedText.Text = "Text Scanned";
            this.TextScannedText.Visible = false;
            // 
            // TextScanned
            // 
            this.TextScanned.AutoSize = true;
            this.TextScanned.Location = new System.Drawing.Point(15, 63);
            this.TextScanned.Name = "TextScanned";
            this.TextScanned.Size = new System.Drawing.Size(35, 13);
            this.TextScanned.TabIndex = 2;
            this.TextScanned.Text = "label1";
            this.TextScanned.Visible = false;
            // 
            // SaveToText
            // 
            this.SaveToText.Location = new System.Drawing.Point(595, 378);
            this.SaveToText.Name = "SaveToText";
            this.SaveToText.Size = new System.Drawing.Size(156, 42);
            this.SaveToText.TabIndex = 3;
            this.SaveToText.Text = "Save To Json Document";
            this.SaveToText.UseVisualStyleBackColor = true;
            this.SaveToText.Visible = false;
            this.SaveToText.Click += new System.EventHandler(this.SaveToText_Click);
            // 
            // TextScanned2
            // 
            this.TextScanned2.AutoSize = true;
            this.TextScanned2.Location = new System.Drawing.Point(294, 63);
            this.TextScanned2.Name = "TextScanned2";
            this.TextScanned2.Size = new System.Drawing.Size(35, 13);
            this.TextScanned2.TabIndex = 4;
            this.TextScanned2.Text = "label1";
            this.TextScanned2.Visible = false;
            // 
            // SaveToCSV
            // 
            this.SaveToCSV.Location = new System.Drawing.Point(406, 378);
            this.SaveToCSV.Name = "SaveToCSV";
            this.SaveToCSV.Size = new System.Drawing.Size(156, 42);
            this.SaveToCSV.TabIndex = 5;
            this.SaveToCSV.Text = "Save To CSV Document";
            this.SaveToCSV.UseVisualStyleBackColor = true;
            this.SaveToCSV.Visible = false;
            this.SaveToCSV.Click += new System.EventHandler(this.SaveToCSV_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.SaveToCSV);
            this.Controls.Add(this.TextScanned2);
            this.Controls.Add(this.SaveToText);
            this.Controls.Add(this.TextScanned);
            this.Controls.Add(this.TextScannedText);
            this.Controls.Add(this.pictureBox1);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label TextScannedText;
        private System.Windows.Forms.Label TextScanned;
        private System.Windows.Forms.Button SaveToText;
        private System.Windows.Forms.Label TextScanned2;
        private System.Windows.Forms.Button SaveToCSV;
    }
}

