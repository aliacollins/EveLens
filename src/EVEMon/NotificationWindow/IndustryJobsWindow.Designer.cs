// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.CharacterMonitoring;

namespace EVEMon.NotificationWindow
{
    partial class IndustryJobsWindow
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
            this.jobsList = new CharacterIndustryJobsList();
            this.SuspendLayout();
            // 
            // jobsList
            // 
            this.jobsList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jobsList.Location = new System.Drawing.Point(0, 0);
            this.jobsList.Name = "jobsList";
            this.jobsList.Size = new System.Drawing.Size(292, 266);
            this.jobsList.TabIndex = 0;
            // 
            // IndustryJobsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.jobsList);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "IndustryJobsWindow";
            this.Text = "Notification Details";
            this.ResumeLayout(false);

        }

        #endregion

        private CharacterIndustryJobsList jobsList;
    }
}
