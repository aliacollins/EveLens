namespace EVEMon.ExceptionHandling
{
    partial class UnhandledExceptionWindow
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
            this.MainPanel = new System.Windows.Forms.Panel();
            this.TechnicalDetailsPanel = new System.Windows.Forms.Panel();
            this.TechnicalDetailsTextBox = new System.Windows.Forms.TextBox();
            this.BugPictureBox = new System.Windows.Forms.PictureBox();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.UserDescriptionLabel = new System.Windows.Forms.Label();
            this.UserDescriptionTextBox = new System.Windows.Forms.TextBox();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.TechnicalDetailsLabel = new System.Windows.Forms.Label();
            this.ButtonPanel = new System.Windows.Forms.Panel();
            this.LatestBinariesLinkLabel = new System.Windows.Forms.LinkLabel();
            this.CloseButton = new System.Windows.Forms.Button();
            this.DataDirectoryButton = new System.Windows.Forms.Button();
            this.CopyDetailsButton = new System.Windows.Forms.Button();
            this.SubmitReportButton = new System.Windows.Forms.Button();
            this.MainPanel.SuspendLayout();
            this.TechnicalDetailsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BugPictureBox)).BeginInit();
            this.ButtonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // MainPanel
            //
            this.MainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainPanel.BackColor = System.Drawing.Color.White;
            this.MainPanel.Controls.Add(this.TechnicalDetailsPanel);
            this.MainPanel.Controls.Add(this.BugPictureBox);
            this.MainPanel.Controls.Add(this.UserDescriptionTextBox);
            this.MainPanel.Controls.Add(this.UserDescriptionLabel);
            this.MainPanel.Controls.Add(this.DescriptionLabel);
            this.MainPanel.Controls.Add(this.TitleLabel);
            this.MainPanel.Controls.Add(this.TechnicalDetailsLabel);
            this.MainPanel.ForeColor = System.Drawing.Color.Black;
            this.MainPanel.Location = new System.Drawing.Point(0, 0);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.Size = new System.Drawing.Size(583, 488);
            this.MainPanel.TabIndex = 0;
            //
            // TechnicalDetailsPanel
            //
            this.TechnicalDetailsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TechnicalDetailsPanel.AutoSize = true;
            this.TechnicalDetailsPanel.Controls.Add(this.TechnicalDetailsTextBox);
            this.TechnicalDetailsPanel.Location = new System.Drawing.Point(12, 144);
            this.TechnicalDetailsPanel.Name = "TechnicalDetailsPanel";
            this.TechnicalDetailsPanel.Size = new System.Drawing.Size(559, 332);
            this.TechnicalDetailsPanel.TabIndex = 13;
            //
            // TechnicalDetailsTextBox
            //
            this.TechnicalDetailsTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TechnicalDetailsTextBox.Location = new System.Drawing.Point(0, 0);
            this.TechnicalDetailsTextBox.Multiline = true;
            this.TechnicalDetailsTextBox.Name = "TechnicalDetailsTextBox";
            this.TechnicalDetailsTextBox.ReadOnly = true;
            this.TechnicalDetailsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TechnicalDetailsTextBox.Size = new System.Drawing.Size(559, 332);
            this.TechnicalDetailsTextBox.TabIndex = 1;
            //
            // BugPictureBox
            //
            this.BugPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BugPictureBox.Location = new System.Drawing.Point(487, 13);
            this.BugPictureBox.Name = "BugPictureBox";
            this.BugPictureBox.Size = new System.Drawing.Size(81, 65);
            this.BugPictureBox.TabIndex = 2;
            this.BugPictureBox.TabStop = false;
            //
            // DescriptionLabel
            //
            this.DescriptionLabel.AutoSize = true;
            this.DescriptionLabel.Location = new System.Drawing.Point(13, 35);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(409, 26);
            this.DescriptionLabel.TabIndex = 12;
            this.DescriptionLabel.Text = "I think Little Fluffy is dead, Jimmy. \r\nEVEMon will be shut down. Restart and try" +
    " again to test whether the problem repeats.";
            //
            // UserDescriptionLabel
            //
            this.UserDescriptionLabel.AutoSize = true;
            this.UserDescriptionLabel.Location = new System.Drawing.Point(13, 66);
            this.UserDescriptionLabel.Name = "UserDescriptionLabel";
            this.UserDescriptionLabel.Size = new System.Drawing.Size(240, 13);
            this.UserDescriptionLabel.TabIndex = 15;
            this.UserDescriptionLabel.Text = "What were you doing when this happened? (optional)";
            //
            // UserDescriptionTextBox
            //
            this.UserDescriptionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.UserDescriptionTextBox.Location = new System.Drawing.Point(16, 82);
            this.UserDescriptionTextBox.Name = "UserDescriptionTextBox";
            this.UserDescriptionTextBox.Size = new System.Drawing.Size(555, 20);
            this.UserDescriptionTextBox.TabIndex = 16;
            //
            // TitleLabel
            //
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Location = new System.Drawing.Point(13, 13);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(233, 13);
            this.TitleLabel.TabIndex = 0;
            this.TitleLabel.Text = "EVEMon has encountered an unexpected error.";
            //
            // TechnicalDetailsLabel
            //
            this.TechnicalDetailsLabel.AutoSize = true;
            this.TechnicalDetailsLabel.Location = new System.Drawing.Point(12, 128);
            this.TechnicalDetailsLabel.Name = "TechnicalDetailsLabel";
            this.TechnicalDetailsLabel.Size = new System.Drawing.Size(145, 13);
            this.TechnicalDetailsLabel.TabIndex = 2;
            this.TechnicalDetailsLabel.Text = "Technical details of this error:";
            //
            // ButtonPanel
            //
            this.ButtonPanel.Controls.Add(this.LatestBinariesLinkLabel);
            this.ButtonPanel.Controls.Add(this.CloseButton);
            this.ButtonPanel.Controls.Add(this.DataDirectoryButton);
            this.ButtonPanel.Controls.Add(this.CopyDetailsButton);
            this.ButtonPanel.Controls.Add(this.SubmitReportButton);
            this.ButtonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ButtonPanel.Location = new System.Drawing.Point(0, 488);
            this.ButtonPanel.Name = "ButtonPanel";
            this.ButtonPanel.Size = new System.Drawing.Size(583, 58);
            this.ButtonPanel.TabIndex = 1;
            //
            // LatestBinariesLinkLabel
            //
            this.LatestBinariesLinkLabel.AutoSize = true;
            this.LatestBinariesLinkLabel.Location = new System.Drawing.Point(0, 40);
            this.LatestBinariesLinkLabel.Name = "LatestBinariesLinkLabel";
            this.LatestBinariesLinkLabel.Size = new System.Drawing.Size(130, 13);
            this.LatestBinariesLinkLabel.TabIndex = 4;
            this.LatestBinariesLinkLabel.TabStop = true;
            this.LatestBinariesLinkLabel.Text = "Download latest version";
            this.LatestBinariesLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LatestBinariesLinkLabel_LinkClicked);
            //
            // CloseButton
            //
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.Location = new System.Drawing.Point(477, 8);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(106, 27);
            this.CloseButton.TabIndex = 3;
            this.CloseButton.Text = "Close EVEMon";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            //
            // DataDirectoryButton
            //
            this.DataDirectoryButton.Location = new System.Drawing.Point(256, 8);
            this.DataDirectoryButton.Name = "DataDirectoryButton";
            this.DataDirectoryButton.Size = new System.Drawing.Size(110, 27);
            this.DataDirectoryButton.TabIndex = 2;
            this.DataDirectoryButton.Text = "Data Directory";
            this.DataDirectoryButton.UseVisualStyleBackColor = true;
            this.DataDirectoryButton.Click += new System.EventHandler(this.DataDirectoryButton_Click);
            //
            // CopyDetailsButton
            //
            this.CopyDetailsButton.Location = new System.Drawing.Point(138, 8);
            this.CopyDetailsButton.Name = "CopyDetailsButton";
            this.CopyDetailsButton.Size = new System.Drawing.Size(110, 27);
            this.CopyDetailsButton.TabIndex = 1;
            this.CopyDetailsButton.Text = "Copy Details";
            this.CopyDetailsButton.UseVisualStyleBackColor = true;
            this.CopyDetailsButton.Click += new System.EventHandler(this.CopyDetailsButton_Click);
            //
            // SubmitReportButton
            //
            this.SubmitReportButton.Location = new System.Drawing.Point(0, 8);
            this.SubmitReportButton.Name = "SubmitReportButton";
            this.SubmitReportButton.Size = new System.Drawing.Size(130, 27);
            this.SubmitReportButton.TabIndex = 0;
            this.SubmitReportButton.Text = "Submit Report";
            this.SubmitReportButton.UseVisualStyleBackColor = true;
            this.SubmitReportButton.Click += new System.EventHandler(this.SubmitReportButton_Click);
            //
            // UnhandledExceptionWindow
            //
            this.AcceptButton = this.CloseButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(583, 546);
            this.ControlBox = false;
            this.Controls.Add(this.MainPanel);
            this.Controls.Add(this.ButtonPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UnhandledExceptionWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EVEMon Error";
            this.Load += new System.EventHandler(this.UnhandledExceptionWindow_Load);
            this.MainPanel.ResumeLayout(false);
            this.MainPanel.PerformLayout();
            this.TechnicalDetailsPanel.ResumeLayout(false);
            this.TechnicalDetailsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BugPictureBox)).EndInit();
            this.ButtonPanel.ResumeLayout(false);
            this.ButtonPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel MainPanel;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.TextBox TechnicalDetailsTextBox;
        private System.Windows.Forms.Label TechnicalDetailsLabel;
        private System.Windows.Forms.PictureBox BugPictureBox;
        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.Label UserDescriptionLabel;
        private System.Windows.Forms.TextBox UserDescriptionTextBox;
        private System.Windows.Forms.Panel TechnicalDetailsPanel;
        private System.Windows.Forms.Panel ButtonPanel;
        private System.Windows.Forms.Button SubmitReportButton;
        private System.Windows.Forms.Button CopyDetailsButton;
        private System.Windows.Forms.Button DataDirectoryButton;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.LinkLabel LatestBinariesLinkLabel;
    }
}
