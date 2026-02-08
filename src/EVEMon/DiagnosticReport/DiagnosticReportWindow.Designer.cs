namespace EVEMon.DiagnosticReport
{
    partial class DiagnosticReportWindow
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
            this.InstructionLabel = new System.Windows.Forms.Label();
            this.ReportTextBox = new System.Windows.Forms.TextBox();
            this.ButtonPanel = new System.Windows.Forms.Panel();
            this.TipLabel = new System.Windows.Forms.Label();
            this.CloseButton = new System.Windows.Forms.Button();
            this.OpenGitHubIssueButton = new System.Windows.Forms.Button();
            this.CopyToClipboardButton = new System.Windows.Forms.Button();
            this.ButtonPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // InstructionLabel
            //
            this.InstructionLabel.AutoSize = true;
            this.InstructionLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.InstructionLabel.Location = new System.Drawing.Point(12, 12);
            this.InstructionLabel.Name = "InstructionLabel";
            this.InstructionLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.InstructionLabel.Size = new System.Drawing.Size(430, 21);
            this.InstructionLabel.TabIndex = 0;
            this.InstructionLabel.Text = "Review the diagnostic report below. Sensitive data has been removed. You may edit" +
    " before sharing.";
            //
            // ReportTextBox
            //
            this.ReportTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ReportTextBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReportTextBox.Location = new System.Drawing.Point(12, 33);
            this.ReportTextBox.MaxLength = 0;
            this.ReportTextBox.Multiline = true;
            this.ReportTextBox.Name = "ReportTextBox";
            this.ReportTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ReportTextBox.Size = new System.Drawing.Size(626, 430);
            this.ReportTextBox.TabIndex = 1;
            this.ReportTextBox.WordWrap = false;
            //
            // ButtonPanel
            //
            this.ButtonPanel.Controls.Add(this.TipLabel);
            this.ButtonPanel.Controls.Add(this.CloseButton);
            this.ButtonPanel.Controls.Add(this.OpenGitHubIssueButton);
            this.ButtonPanel.Controls.Add(this.CopyToClipboardButton);
            this.ButtonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ButtonPanel.Location = new System.Drawing.Point(12, 463);
            this.ButtonPanel.Name = "ButtonPanel";
            this.ButtonPanel.Size = new System.Drawing.Size(626, 65);
            this.ButtonPanel.TabIndex = 2;
            //
            // TipLabel
            //
            this.TipLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.TipLabel.AutoSize = true;
            this.TipLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.TipLabel.Location = new System.Drawing.Point(0, 48);
            this.TipLabel.Name = "TipLabel";
            this.TipLabel.Size = new System.Drawing.Size(370, 13);
            this.TipLabel.TabIndex = 3;
            this.TipLabel.Text = "Tip: Review the report for any remaining personal information before sharing.";
            //
            // CloseButton
            //
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseButton.Location = new System.Drawing.Point(548, 8);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 27);
            this.CloseButton.TabIndex = 2;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            //
            // OpenGitHubIssueButton
            //
            this.OpenGitHubIssueButton.Location = new System.Drawing.Point(130, 8);
            this.OpenGitHubIssueButton.Name = "OpenGitHubIssueButton";
            this.OpenGitHubIssueButton.Size = new System.Drawing.Size(130, 27);
            this.OpenGitHubIssueButton.TabIndex = 1;
            this.OpenGitHubIssueButton.Text = "Submit Report";
            this.OpenGitHubIssueButton.UseVisualStyleBackColor = true;
            this.OpenGitHubIssueButton.Click += new System.EventHandler(this.OpenGitHubIssueButton_Click);
            //
            // CopyToClipboardButton
            //
            this.CopyToClipboardButton.Location = new System.Drawing.Point(0, 8);
            this.CopyToClipboardButton.Name = "CopyToClipboardButton";
            this.CopyToClipboardButton.Size = new System.Drawing.Size(120, 27);
            this.CopyToClipboardButton.TabIndex = 0;
            this.CopyToClipboardButton.Text = "Copy to Clipboard";
            this.CopyToClipboardButton.UseVisualStyleBackColor = true;
            this.CopyToClipboardButton.Click += new System.EventHandler(this.CopyToClipboardButton_Click);
            //
            // DiagnosticReportWindow
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CloseButton;
            this.ClientSize = new System.Drawing.Size(650, 540);
            this.Controls.Add(this.ReportTextBox);
            this.Controls.Add(this.ButtonPanel);
            this.Controls.Add(this.InstructionLabel);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "DiagnosticReportWindow";
            this.Padding = new System.Windows.Forms.Padding(12);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Diagnostic Report";
            this.Load += new System.EventHandler(this.DiagnosticReportWindow_Load);
            this.ButtonPanel.ResumeLayout(false);
            this.ButtonPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label InstructionLabel;
        private System.Windows.Forms.TextBox ReportTextBox;
        private System.Windows.Forms.Panel ButtonPanel;
        private System.Windows.Forms.Label TipLabel;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Button OpenGitHubIssueButton;
        private System.Windows.Forms.Button CopyToClipboardButton;
    }
}
