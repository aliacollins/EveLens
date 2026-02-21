// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Enumerations.UISettings;

namespace EVEMon.NotificationWindow
{
    partial class PlanetaryPinsWindow
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
            this.planetaryList = new EVEMon.CharacterMonitoring.CharacterPlanetaryList();
            this.SuspendLayout();
            // 
            // planetaryList
            // 
            this.planetaryList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.planetaryList.Grouping = PlanetaryGrouping.None;
            this.planetaryList.Location = new System.Drawing.Point(0, 0);
            this.planetaryList.Name = "planetaryList";
            this.planetaryList.Size = new System.Drawing.Size(292, 266);
            this.planetaryList.TabIndex = 0;
            this.planetaryList.TextFilter = "";
            // 
            // PlanetaryPinsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.planetaryList);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlanetaryPinsWindow";
            this.Text = "Notification Details";
            this.ResumeLayout(false);

        }

        #endregion

        private CharacterMonitoring.CharacterPlanetaryList planetaryList;
    }
}