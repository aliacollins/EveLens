// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.CharacterMonitoring;
using EVEMon.Common.Enumerations;
using EVEMon.Controls;

namespace EVEMon.DetailsWindow
{
    partial class EveMessageWindow
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
            this.throbber = new EVEMon.Common.Controls.Throbber();
            this.readingPane = new ReadingPane();
            ((System.ComponentModel.ISupportInitialize)(this.throbber)).BeginInit();
            this.SuspendLayout();
            // 
            // throbber
            // 
            this.throbber.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.throbber.Location = new System.Drawing.Point(195, 104);
            this.throbber.MaximumSize = new System.Drawing.Size(24, 24);
            this.throbber.MinimumSize = new System.Drawing.Size(24, 24);
            this.throbber.Name = "throbber";
            this.throbber.Size = new System.Drawing.Size(24, 24);
            this.throbber.State = ThrobberState.Stopped;
            this.throbber.TabIndex = 1;
            this.throbber.TabStop = false;
            // 
            // readingPane
            // 
            this.readingPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this.readingPane.Location = new System.Drawing.Point(0, 0);
            this.readingPane.MinimumSize = new System.Drawing.Size(250, 150);
            this.readingPane.Name = "readingPane";
            this.readingPane.Size = new System.Drawing.Size(414, 232);
            this.readingPane.TabIndex = 0;
            // 
            // EveMessageWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(414, 232);
            this.Controls.Add(this.readingPane);
            this.Controls.Add(this.throbber);
            this.MinimumSize = new System.Drawing.Size(400, 250);
            this.Name = "EveMessageWindow";
            this.Text = "EVE Mail Message";
            ((System.ComponentModel.ISupportInitialize)(this.throbber)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private ReadingPane readingPane;
        private Common.Controls.Throbber throbber;
    }
}
