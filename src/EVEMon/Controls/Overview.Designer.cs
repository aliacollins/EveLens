// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Controls
{
    partial class Overview
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelNoCharacters = new System.Windows.Forms.Label();
            this.labelLoading = new System.Windows.Forms.Label();
            this.overviewLoadingThrobber = new EVEMon.Common.Controls.Throbber();
            ((System.ComponentModel.ISupportInitialize)(this.overviewLoadingThrobber)).BeginInit();
            this.SuspendLayout();
            // 
            // labelNoCharacters
            // 
            this.labelNoCharacters.BackColor = System.Drawing.Color.Transparent;
            this.labelNoCharacters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelNoCharacters.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelNoCharacters.Location = new System.Drawing.Point(0, 0);
            this.labelNoCharacters.Name = "labelNoCharacters";
            this.labelNoCharacters.Size = new System.Drawing.Size(361, 507);
            this.labelNoCharacters.TabIndex = 0;
            this.labelNoCharacters.Text = "No character loaded or monitored\r\n\r\nTo add characters, click the File > Add API k" +
    "ey... menu option\r\nTo monitor characters, click the File > Manage API Keys... me" +
    "nu option";
            this.labelNoCharacters.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelNoCharacters.Visible = false;
            // 
            // labelLoading
            // 
            this.labelLoading.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelLoading.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelLoading.Location = new System.Drawing.Point(0, 0);
            this.labelLoading.Name = "labelLoading";
            this.labelLoading.Size = new System.Drawing.Size(361, 507);
            this.labelLoading.TabIndex = 1;
            this.labelLoading.Text = "Loading...\r\n\r\nPlease Wait.";
            this.labelLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // overviewLoadingThrobber
            // 
            this.overviewLoadingThrobber.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.overviewLoadingThrobber.Location = new System.Drawing.Point(168, 243);
            this.overviewLoadingThrobber.MaximumSize = new System.Drawing.Size(24, 24);
            this.overviewLoadingThrobber.MinimumSize = new System.Drawing.Size(24, 24);
            this.overviewLoadingThrobber.Name = "overviewLoadingThrobber";
            this.overviewLoadingThrobber.Size = new System.Drawing.Size(24, 24);
            this.overviewLoadingThrobber.State = EVEMon.Common.Enumerations.ThrobberState.Stopped;
            this.overviewLoadingThrobber.TabIndex = 2;
            this.overviewLoadingThrobber.TabStop = false;
            this.overviewLoadingThrobber.Visible = false;
            // 
            // Overview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Controls.Add(this.labelLoading);
            this.Controls.Add(this.labelNoCharacters);
            this.Controls.Add(this.overviewLoadingThrobber);
            this.Name = "Overview";
            this.Size = new System.Drawing.Size(361, 507);
            ((System.ComponentModel.ISupportInitialize)(this.overviewLoadingThrobber)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelNoCharacters;
        private System.Windows.Forms.Label labelLoading;
        private Common.Controls.Throbber overviewLoadingThrobber;
    }
}
