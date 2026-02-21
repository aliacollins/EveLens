// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Enumerations;

namespace EVEMon.Common.Controls
{
    partial class EveImagePopUp
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
            this.eveImage = new EVEMon.Common.Controls.EveImage();
            this.SuspendLayout();
            // 
            // eveImage
            // 
            this.eveImage.ImageSize = EveImageSize.x256;
            this.eveImage.Location = new System.Drawing.Point(0, 0);
            this.eveImage.Name = "eveImage";
            this.eveImage.PopUpEnabled = false;
            this.eveImage.Size = new System.Drawing.Size(256, 256);
            this.eveImage.TabIndex = 0;
            // 
            // EveImagePopUp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(256, 256);
            this.Controls.Add(this.eveImage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.Name = "EveImagePopUp";
            this.Text = "EveImagePopUp";
            this.ResumeLayout(false);

        }

        #endregion

        private EveImage eveImage;

    }
}
