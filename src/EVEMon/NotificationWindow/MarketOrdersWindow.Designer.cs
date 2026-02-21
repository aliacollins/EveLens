// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.CharacterMonitoring;

namespace EVEMon.NotificationWindow
{
    partial class MarketOrdersWindow
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
            this.ordersList = new CharacterMarketOrdersList();
            this.SuspendLayout();
            // 
            // ordersList
            // 
            this.ordersList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ordersList.Location = new System.Drawing.Point(0, 0);
            this.ordersList.Name = "ordersList";
            this.ordersList.Size = new System.Drawing.Size(292, 266);
            this.ordersList.TabIndex = 0;
            // 
            // MarketOrdersWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.ordersList);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MarketOrdersWindow";
            this.Text = "Notification Details";
            this.ResumeLayout(false);

        }

        #endregion

        private CharacterMarketOrdersList ordersList;
    }
}