namespace TelemetryProject {
    partial class AboutForm {
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
            this.about1Label = new System.Windows.Forms.Label();
            this.about4Label = new System.Windows.Forms.Label();
            this.about3Label = new System.Windows.Forms.Label();
            this.about2Label = new System.Windows.Forms.Label();
            this.aboutVersionLabel = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // about1Label
            // 
            this.about1Label.AutoSize = true;
            this.about1Label.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.about1Label.Location = new System.Drawing.Point(173, 166);
            this.about1Label.Name = "about1Label";
            this.about1Label.Size = new System.Drawing.Size(192, 25);
            this.about1Label.TabIndex = 0;
            this.about1Label.Text = "Telemetry Console";
            // 
            // about4Label
            // 
            this.about4Label.AutoSize = true;
            this.about4Label.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.about4Label.Location = new System.Drawing.Point(20, 380);
            this.about4Label.Name = "about4Label";
            this.about4Label.Size = new System.Drawing.Size(518, 25);
            this.about4Label.TabIndex = 1;
            this.about4Label.Text = "Contact for more information: danielxp75@gmail.com";
            // 
            // about3Label
            // 
            this.about3Label.AutoSize = true;
            this.about3Label.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.about3Label.Location = new System.Drawing.Point(144, 355);
            this.about3Label.Name = "about3Label";
            this.about3Label.Size = new System.Drawing.Size(261, 25);
            this.about3Label.TabIndex = 2;
            this.about3Label.Text = "Designed by Dean Zadok ";
            // 
            // about2Label
            // 
            this.about2Label.AutoSize = true;
            this.about2Label.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.about2Label.Location = new System.Drawing.Point(40, 191);
            this.about2Label.Name = "about2Label";
            this.about2Label.Size = new System.Drawing.Size(481, 25);
            this.about2Label.TabIndex = 3;
            this.about2Label.Text = "For use only for Technion Formula Student Team";
            // 
            // aboutVersionLabel
            // 
            this.aboutVersionLabel.AutoSize = true;
            this.aboutVersionLabel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.aboutVersionLabel.Location = new System.Drawing.Point(184, 242);
            this.aboutVersionLabel.Name = "aboutVersionLabel";
            this.aboutVersionLabel.Size = new System.Drawing.Size(91, 25);
            this.aboutVersionLabel.TabIndex = 4;
            this.aboutVersionLabel.Text = "Version ";
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(217, 438);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(116, 42);
            this.closeButton.TabIndex = 5;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Image = global::TelemetryProject.Properties.Resources.FormulaLogoWhite;
            this.logoPictureBox.Location = new System.Drawing.Point(149, 26);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(256, 119);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.logoPictureBox.TabIndex = 6;
            this.logoPictureBox.TabStop = false;
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.ControlText;
            this.ClientSize = new System.Drawing.Size(556, 492);
            this.Controls.Add(this.logoPictureBox);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.aboutVersionLabel);
            this.Controls.Add(this.about2Label);
            this.Controls.Add(this.about3Label);
            this.Controls.Add(this.about4Label);
            this.Controls.Add(this.about1Label);
            this.Name = "AboutForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label about1Label;
        private System.Windows.Forms.Label about4Label;
        private System.Windows.Forms.Label about3Label;
        private System.Windows.Forms.Label about2Label;
        private System.Windows.Forms.Label aboutVersionLabel;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.PictureBox logoPictureBox;
    }
}