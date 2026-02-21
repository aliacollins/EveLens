// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.CharacterMonitoring
{
    internal sealed partial class CharacterMonitorFooter
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
            this.skillQueuePanel = new System.Windows.Forms.Panel();
            this.lblPaused = new System.Windows.Forms.Label();
            this.skillQueueTimePanel = new System.Windows.Forms.Panel();
            this.lblQueueCompletionTime = new System.Windows.Forms.Label();
            this.lblQueueRemaining = new System.Windows.Forms.Label();
            this.skillQueueControl = new EVEMon.Common.Controls.SkillQueueControl();
            this.pnlTraining = new System.Windows.Forms.Panel();
            this.tlpStatus = new System.Windows.Forms.TableLayoutPanel();
            this.flpStatusLabels = new System.Windows.Forms.FlowLayoutPanel();
            this.lblCurrentlyTraining = new System.Windows.Forms.Label();
            this.lblSPPerHour = new System.Windows.Forms.Label();
            this.lblScheduleWarning = new System.Windows.Forms.Label();
            this.flpStatusValues = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTrainingSkill = new System.Windows.Forms.Label();
            this.lblTrainingRemain = new System.Windows.Forms.Label();
            this.lblTrainingEst = new System.Windows.Forms.Label();
            this.btnAddToCalendar = new System.Windows.Forms.Button();
            this.skillQueuePanel.SuspendLayout();
            this.skillQueueTimePanel.SuspendLayout();
            this.pnlTraining.SuspendLayout();
            this.tlpStatus.SuspendLayout();
            this.flpStatusLabels.SuspendLayout();
            this.flpStatusValues.SuspendLayout();
            this.SuspendLayout();
            // 
            // skillQueuePanel
            // 
            this.skillQueuePanel.AutoSize = true;
            this.skillQueuePanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.skillQueuePanel.Controls.Add(this.lblPaused);
            this.skillQueuePanel.Controls.Add(this.skillQueueTimePanel);
            this.skillQueuePanel.Controls.Add(this.skillQueueControl);
            this.skillQueuePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.skillQueuePanel.Location = new System.Drawing.Point(0, 0);
            this.skillQueuePanel.Name = "skillQueuePanel";
            this.skillQueuePanel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.skillQueuePanel.Size = new System.Drawing.Size(463, 56);
            this.skillQueuePanel.TabIndex = 0;
            // 
            // lblPaused
            // 
            this.lblPaused.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblPaused.Location = new System.Drawing.Point(0, 23);
            this.lblPaused.Name = "lblPaused";
            this.lblPaused.Size = new System.Drawing.Size(463, 17);
            this.lblPaused.TabIndex = 0;
            this.lblPaused.Text = "Paused";
            this.lblPaused.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // skillQueueTimePanel
            // 
            this.skillQueueTimePanel.Controls.Add(this.lblQueueCompletionTime);
            this.skillQueueTimePanel.Controls.Add(this.lblQueueRemaining);
            this.skillQueueTimePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.skillQueueTimePanel.Location = new System.Drawing.Point(0, 6);
            this.skillQueueTimePanel.Name = "skillQueueTimePanel";
            this.skillQueueTimePanel.Size = new System.Drawing.Size(463, 17);
            this.skillQueueTimePanel.TabIndex = 0;
            // 
            // lblQueueCompletionTime
            // 
            this.lblQueueCompletionTime.AutoSize = true;
            this.lblQueueCompletionTime.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblQueueCompletionTime.Location = new System.Drawing.Point(343, 0);
            this.lblQueueCompletionTime.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblQueueCompletionTime.Name = "lblQueueCompletionTime";
            this.lblQueueCompletionTime.Size = new System.Drawing.Size(120, 13);
            this.lblQueueCompletionTime.TabIndex = 1;
            this.lblQueueCompletionTime.Text = "Queue Completion Time";
            // 
            // lblQueueRemaining
            // 
            this.lblQueueRemaining.AutoSize = true;
            this.lblQueueRemaining.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblQueueRemaining.Location = new System.Drawing.Point(0, 0);
            this.lblQueueRemaining.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblQueueRemaining.Name = "lblQueueRemaining";
            this.lblQueueRemaining.Size = new System.Drawing.Size(118, 13);
            this.lblQueueRemaining.TabIndex = 0;
            this.lblQueueRemaining.Text = "Queue Remaining Time";
            // 
            // skillQueueControl
            // 
            this.skillQueueControl.BackColor = System.Drawing.SystemColors.Control;
            this.skillQueueControl.BorderColor = System.Drawing.Color.Gray;
            this.skillQueueControl.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.skillQueueControl.EmptyColor = System.Drawing.Color.DimGray;
            this.skillQueueControl.LessThanDayFirstColor = System.Drawing.Color.Khaki;
            this.skillQueueControl.LessThanDaySecondColor = System.Drawing.Color.DarkKhaki;
            this.skillQueueControl.Location = new System.Drawing.Point(0, 40);
            this.skillQueueControl.MoreThanDayFirstColor = System.Drawing.Color.LightSteelBlue;
            this.skillQueueControl.MoreThanDaySecondColor = System.Drawing.Color.LightSlateGray;
            this.skillQueueControl.Name = "skillQueueControl";
            this.skillQueueControl.Size = new System.Drawing.Size(463, 10);
            this.skillQueueControl.SkillQueue = null;
            this.skillQueueControl.TabIndex = 0;
            // 
            // pnlTraining
            // 
            this.pnlTraining.AutoSize = true;
            this.pnlTraining.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlTraining.Controls.Add(this.tlpStatus);
            this.pnlTraining.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTraining.Location = new System.Drawing.Point(0, 56);
            this.pnlTraining.Name = "pnlTraining";
            this.pnlTraining.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.pnlTraining.Size = new System.Drawing.Size(463, 44);
            this.pnlTraining.TabIndex = 1;
            // 
            // tlpStatus
            // 
            this.tlpStatus.AutoSize = true;
            this.tlpStatus.ColumnCount = 3;
            this.tlpStatus.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpStatus.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpStatus.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpStatus.Controls.Add(this.flpStatusLabels, 0, 0);
            this.tlpStatus.Controls.Add(this.flpStatusValues, 1, 0);
            this.tlpStatus.Controls.Add(this.btnAddToCalendar, 2, 0);
            this.tlpStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpStatus.Location = new System.Drawing.Point(0, 3);
            this.tlpStatus.Margin = new System.Windows.Forms.Padding(0);
            this.tlpStatus.Name = "tlpStatus";
            this.tlpStatus.RowCount = 1;
            this.tlpStatus.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpStatus.Size = new System.Drawing.Size(463, 41);
            this.tlpStatus.TabIndex = 0;
            // 
            // flpStatusLabels
            // 
            this.flpStatusLabels.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flpStatusLabels.AutoSize = true;
            this.flpStatusLabels.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpStatusLabels.Controls.Add(this.lblCurrentlyTraining);
            this.flpStatusLabels.Controls.Add(this.lblSPPerHour);
            this.flpStatusLabels.Controls.Add(this.lblScheduleWarning);
            this.flpStatusLabels.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpStatusLabels.Location = new System.Drawing.Point(0, 0);
            this.flpStatusLabels.Margin = new System.Windows.Forms.Padding(0);
            this.flpStatusLabels.Name = "flpStatusLabels";
            this.flpStatusLabels.Size = new System.Drawing.Size(96, 41);
            this.flpStatusLabels.TabIndex = 0;
            this.flpStatusLabels.WrapContents = false;
            // 
            // lblCurrentlyTraining
            // 
            this.lblCurrentlyTraining.AutoSize = true;
            this.lblCurrentlyTraining.Location = new System.Drawing.Point(0, 0);
            this.lblCurrentlyTraining.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblCurrentlyTraining.Name = "lblCurrentlyTraining";
            this.lblCurrentlyTraining.Size = new System.Drawing.Size(92, 13);
            this.lblCurrentlyTraining.TabIndex = 2;
            this.lblCurrentlyTraining.Text = "Currently Training:";
            // 
            // lblSPPerHour
            // 
            this.lblSPPerHour.AutoSize = true;
            this.lblSPPerHour.Location = new System.Drawing.Point(0, 13);
            this.lblSPPerHour.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblSPPerHour.Name = "lblSPPerHour";
            this.lblSPPerHour.Size = new System.Drawing.Size(59, 13);
            this.lblSPPerHour.TabIndex = 0;
            this.lblSPPerHour.Text = "X SP/Hour";
            // 
            // lblScheduleWarning
            // 
            this.lblScheduleWarning.AutoSize = true;
            this.lblScheduleWarning.ForeColor = System.Drawing.Color.Red;
            this.lblScheduleWarning.Location = new System.Drawing.Point(0, 26);
            this.lblScheduleWarning.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblScheduleWarning.Name = "lblScheduleWarning";
            this.lblScheduleWarning.Size = new System.Drawing.Size(93, 13);
            this.lblScheduleWarning.TabIndex = 1;
            this.lblScheduleWarning.Text = "Schedule Conflict!";
            // 
            // flpStatusValues
            // 
            this.flpStatusValues.AutoSize = true;
            this.flpStatusValues.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpStatusValues.Controls.Add(this.lblTrainingSkill);
            this.flpStatusValues.Controls.Add(this.lblTrainingRemain);
            this.flpStatusValues.Controls.Add(this.lblTrainingEst);
            this.flpStatusValues.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpStatusValues.Location = new System.Drawing.Point(96, 0);
            this.flpStatusValues.Margin = new System.Windows.Forms.Padding(0);
            this.flpStatusValues.Name = "flpStatusValues";
            this.flpStatusValues.Size = new System.Drawing.Size(145, 39);
            this.flpStatusValues.TabIndex = 1;
            // 
            // lblTrainingSkill
            // 
            this.lblTrainingSkill.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblTrainingSkill.AutoSize = true;
            this.lblTrainingSkill.Location = new System.Drawing.Point(0, 0);
            this.lblTrainingSkill.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblTrainingSkill.Name = "lblTrainingSkill";
            this.lblTrainingSkill.Size = new System.Drawing.Size(67, 13);
            this.lblTrainingSkill.TabIndex = 0;
            this.lblTrainingSkill.Text = "Training Skill";
            // 
            // lblTrainingRemain
            // 
            this.lblTrainingRemain.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblTrainingRemain.AutoSize = true;
            this.lblTrainingRemain.Location = new System.Drawing.Point(0, 13);
            this.lblTrainingRemain.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblTrainingRemain.Name = "lblTrainingRemain";
            this.lblTrainingRemain.Size = new System.Drawing.Size(124, 13);
            this.lblTrainingRemain.TabIndex = 1;
            this.lblTrainingRemain.Text = "Remaining Training Time";
            // 
            // lblTrainingEst
            // 
            this.lblTrainingEst.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblTrainingEst.AutoSize = true;
            this.lblTrainingEst.Location = new System.Drawing.Point(0, 26);
            this.lblTrainingEst.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.lblTrainingEst.Name = "lblTrainingEst";
            this.lblTrainingEst.Size = new System.Drawing.Size(142, 13);
            this.lblTrainingEst.TabIndex = 2;
            this.lblTrainingEst.Text = "Estimated Training End Time";
            // 
            // btnAddToCalendar
            // 
            this.btnAddToCalendar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddToCalendar.Location = new System.Drawing.Point(351, 15);
            this.btnAddToCalendar.Name = "btnAddToCalendar";
            this.btnAddToCalendar.Size = new System.Drawing.Size(109, 23);
            this.btnAddToCalendar.TabIndex = 0;
            this.btnAddToCalendar.Text = "Update Calendar";
            this.btnAddToCalendar.UseVisualStyleBackColor = true;
            this.btnAddToCalendar.Click += new System.EventHandler(this.btnUpdateCalendar_Click);
            // 
            // CharacterMonitorFooter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlTraining);
            this.Controls.Add(this.skillQueuePanel);
            this.Name = "CharacterMonitorFooter";
            this.Size = new System.Drawing.Size(463, 100);
            this.skillQueuePanel.ResumeLayout(false);
            this.skillQueueTimePanel.ResumeLayout(false);
            this.skillQueueTimePanel.PerformLayout();
            this.pnlTraining.ResumeLayout(false);
            this.pnlTraining.PerformLayout();
            this.tlpStatus.ResumeLayout(false);
            this.tlpStatus.PerformLayout();
            this.flpStatusLabels.ResumeLayout(false);
            this.flpStatusLabels.PerformLayout();
            this.flpStatusValues.ResumeLayout(false);
            this.flpStatusValues.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel skillQueuePanel;
        private System.Windows.Forms.Label lblPaused;
        private System.Windows.Forms.Panel skillQueueTimePanel;
        private System.Windows.Forms.Label lblQueueCompletionTime;
        private System.Windows.Forms.Label lblQueueRemaining;
        private Common.Controls.SkillQueueControl skillQueueControl;
        private System.Windows.Forms.Panel pnlTraining;
        private System.Windows.Forms.TableLayoutPanel tlpStatus;
        private System.Windows.Forms.FlowLayoutPanel flpStatusLabels;
        private System.Windows.Forms.Label lblCurrentlyTraining;
        private System.Windows.Forms.Label lblSPPerHour;
        private System.Windows.Forms.Label lblScheduleWarning;
        private System.Windows.Forms.FlowLayoutPanel flpStatusValues;
        private System.Windows.Forms.Label lblTrainingSkill;
        private System.Windows.Forms.Label lblTrainingRemain;
        private System.Windows.Forms.Label lblTrainingEst;
        private System.Windows.Forms.Button btnAddToCalendar;
    }
}
