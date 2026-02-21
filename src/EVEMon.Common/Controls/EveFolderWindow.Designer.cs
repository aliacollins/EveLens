// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Controls
{
    partial class EveFolderWindow
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
            this.BrowseButton = new System.Windows.Forms.Button();
            this.FilenameTextBox = new System.Windows.Forms.TextBox();
            this.OpenDirFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.CancelButon = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.DefaultFolderRadioButton = new System.Windows.Forms.RadioButton();
            this.SpecifyFolderRadioButton = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // BrowseButton
            // 
            this.BrowseButton.Enabled = false;
            this.BrowseButton.Location = new System.Drawing.Point(268, 58);
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.Size = new System.Drawing.Size(75, 23);
            this.BrowseButton.TabIndex = 3;
            this.BrowseButton.Text = "Browse...";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // FilenameTextBox
            // 
            this.FilenameTextBox.Enabled = false;
            this.FilenameTextBox.Location = new System.Drawing.Point(43, 60);
            this.FilenameTextBox.Name = "FilenameTextBox";
            this.FilenameTextBox.ReadOnly = true;
            this.FilenameTextBox.Size = new System.Drawing.Size(219, 20);
            this.FilenameTextBox.TabIndex = 2;
            // 
            // OpenDirFolderBrowserDialog
            // 
            this.OpenDirFolderBrowserDialog.Description = "Select the folder that contains EVE Portraits.";
            // 
            // CancelButon
            // 
            this.CancelButon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelButon.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelButon.Location = new System.Drawing.Point(268, 103);
            this.CancelButon.Name = "CancelButon";
            this.CancelButon.Size = new System.Drawing.Size(75, 23);
            this.CancelButon.TabIndex = 5;
            this.CancelButon.Text = "Cancel";
            this.CancelButon.UseVisualStyleBackColor = true;
            // 
            // OKButton
            // 
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.Enabled = false;
            this.OKButton.Location = new System.Drawing.Point(187, 103);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 4;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // DefaultFolderRadioButton
            // 
            this.DefaultFolderRadioButton.AutoSize = true;
            this.DefaultFolderRadioButton.Checked = true;
            this.DefaultFolderRadioButton.Location = new System.Drawing.Point(13, 13);
            this.DefaultFolderRadioButton.Name = "DefaultFolderRadioButton";
            this.DefaultFolderRadioButton.Size = new System.Drawing.Size(126, 17);
            this.DefaultFolderRadioButton.TabIndex = 0;
            this.DefaultFolderRadioButton.TabStop = true;
            this.DefaultFolderRadioButton.Text = "&Use the default folder";
            this.DefaultFolderRadioButton.UseVisualStyleBackColor = true;
            this.DefaultFolderRadioButton.CheckedChanged += new System.EventHandler(this.DefaultFolderRadioButton_CheckedChanged);
            // 
            // SpecifyFolderRadioButton
            // 
            this.SpecifyFolderRadioButton.AutoSize = true;
            this.SpecifyFolderRadioButton.Location = new System.Drawing.Point(13, 37);
            this.SpecifyFolderRadioButton.Name = "SpecifyFolderRadioButton";
            this.SpecifyFolderRadioButton.Size = new System.Drawing.Size(98, 17);
            this.SpecifyFolderRadioButton.TabIndex = 1;
            this.SpecifyFolderRadioButton.Text = "&Specify a folder";
            this.SpecifyFolderRadioButton.UseVisualStyleBackColor = true;
            this.SpecifyFolderRadioButton.CheckedChanged += new System.EventHandler(this.SpecifyFolderRadioButton_CheckedChanged);
            // 
            // EveFolderWindow
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelButon;
            this.ClientSize = new System.Drawing.Size(355, 138);
            this.Controls.Add(this.SpecifyFolderRadioButton);
            this.Controls.Add(this.DefaultFolderRadioButton);
            this.Controls.Add(this.BrowseButton);
            this.Controls.Add(this.FilenameTextBox);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.CancelButon);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EveFolderWindow";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EVE Portraits Folder";
            this.Load += new System.EventHandler(this.EVEFolderWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.TextBox FilenameTextBox;
        private System.Windows.Forms.FolderBrowserDialog OpenDirFolderBrowserDialog;
        private System.Windows.Forms.Button CancelButon;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.RadioButton DefaultFolderRadioButton;
        private System.Windows.Forms.RadioButton SpecifyFolderRadioButton;

    }
}
