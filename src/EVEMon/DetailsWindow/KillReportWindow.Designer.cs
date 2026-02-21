// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.DetailsWindow
{
    partial class KillReportWindow
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
            this.killReportFittingContent = new EVEMon.Controls.KillReportFittingContent();
            this.killReportInvolvedParties = new EVEMon.Controls.KillReportInvolvedParties();
            this.killReportVictim = new EVEMon.Controls.KillReportVictim();
            this.SuspendLayout();
            // 
            // killReportFittingContent
            // 
            this.killReportFittingContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.killReportFittingContent.Location = new System.Drawing.Point(262, 142);
            this.killReportFittingContent.Name = "killReportFittingContent";
            this.killReportFittingContent.Size = new System.Drawing.Size(322, 440);
            this.killReportFittingContent.TabIndex = 2;
            // 
            // killReportInvolvedParties
            // 
            this.killReportInvolvedParties.Dock = System.Windows.Forms.DockStyle.Left;
            this.killReportInvolvedParties.Location = new System.Drawing.Point(0, 142);
            this.killReportInvolvedParties.Name = "killReportInvolvedParties";
            this.killReportInvolvedParties.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.killReportInvolvedParties.Size = new System.Drawing.Size(262, 440);
            this.killReportInvolvedParties.TabIndex = 1;
            // 
            // killReportVictim
            // 
            this.killReportVictim.AutoSize = true;
            this.killReportVictim.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.killReportVictim.Dock = System.Windows.Forms.DockStyle.Top;
            this.killReportVictim.Location = new System.Drawing.Point(0, 0);
            this.killReportVictim.Name = "killReportVictim";
            this.killReportVictim.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.killReportVictim.Size = new System.Drawing.Size(584, 142);
            this.killReportVictim.TabIndex = 0;
            // 
            // KillReportWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 582);
            this.Controls.Add(this.killReportFittingContent);
            this.Controls.Add(this.killReportInvolvedParties);
            this.Controls.Add(this.killReportVictim);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 620);
            this.Name = "KillReportWindow";
            this.Text = "Kill Report";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Controls.KillReportVictim killReportVictim;
        private Controls.KillReportInvolvedParties killReportInvolvedParties;
        private Controls.KillReportFittingContent killReportFittingContent;

    }
}