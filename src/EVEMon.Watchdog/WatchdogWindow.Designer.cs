// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using EVEMon.Watchdog.Properties;

namespace EVEMon.Watchdog
{
    partial class WatchdogWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.components = new Container();
            this.TimerPictureBox = new PictureBox();
            this.StatusLabel = new Label();
            this.WaitTimer = new Timer(this.components);
            ((ISupportInitialize)(this.TimerPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // TimerPictureBox
            // 
            this.TimerPictureBox.Image = Resources.TimeIcon;
            this.TimerPictureBox.Location = new Point(12, 12);
            this.TimerPictureBox.Name = "TimerPictureBox";
            this.TimerPictureBox.Size = new Size(16, 16);
            this.TimerPictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            this.TimerPictureBox.TabIndex = 0;
            this.TimerPictureBox.TabStop = false;
            // 
            // StatusLabel
            // 
            this.StatusLabel.AutoSize = true;
            this.StatusLabel.Location = new Point(35, 14);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new Size(146, 13);
            this.StatusLabel.TabIndex = 1;
            this.StatusLabel.Text = "Waiting for EVEMon to close.";
            // 
            // WaitTimer
            // 
            this.WaitTimer.Interval = 1000;
            this.WaitTimer.Tick += new EventHandler(this.WaitTimer_Tick);
            // 
            // WatchdogWindow
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(286, 41);
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this.TimerPictureBox);
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.Name = "WatchdogWindow";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "EVEMon Watchdog";
            this.Load += new EventHandler(this.WatchdogWindow_Load);
            ((ISupportInitialize)(this.TimerPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PictureBox TimerPictureBox;
        private Label StatusLabel;
        private Timer WaitTimer;
    }
}

