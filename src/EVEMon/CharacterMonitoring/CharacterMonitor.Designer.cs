// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.CharacterMonitoring
{
    internal partial class CharacterMonitor
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
            this.Footer = new EVEMon.CharacterMonitoring.CharacterMonitorFooter();
            this.Body = new EVEMon.CharacterMonitoring.CharacterMonitorBody();
            this.Header = new EVEMon.CharacterMonitoring.CharacterMonitorHeader();
            this.SuspendLayout();
            // 
            // Footer
            // 
            this.Footer.AutoSize = true;
            this.Footer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Footer.Location = new System.Drawing.Point(0, 591);
            this.Footer.Name = "Footer";
            this.Footer.Size = new System.Drawing.Size(614, 98);
            this.Footer.TabIndex = 2;
            // 
            // Body
            // 
            this.Body.AutoSize = true;
            this.Body.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Body.Location = new System.Drawing.Point(0, 166);
            this.Body.Name = "Body";
            this.Body.Size = new System.Drawing.Size(614, 425);
            this.Body.TabIndex = 1;
            // 
            // Header
            // 
            this.Header.AutoSize = true;
            this.Header.Dock = System.Windows.Forms.DockStyle.Top;
            this.Header.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.Header.Location = new System.Drawing.Point(0, 0);
            this.Header.Name = "Header";
            this.Header.Size = new System.Drawing.Size(614, 166);
            this.Header.TabIndex = 0;
            // 
            // CharacterMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.Body);
            this.Controls.Add(this.Footer);
            this.Controls.Add(this.Header);
            this.Name = "CharacterMonitor";
            this.Size = new System.Drawing.Size(614, 689);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private CharacterMonitorHeader Header;
        private CharacterMonitorBody Body;
        private CharacterMonitorFooter Footer;
    }
}