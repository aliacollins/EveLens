// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Controls
{
    partial class KillReportInvolvedParties
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
            this.MainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.InvolvedPartiesLabel = new System.Windows.Forms.Label();
            this.TotalDamageTakenLabel = new System.Windows.Forms.Label();
            this.FinalBlowLabel = new System.Windows.Forms.Label();
            this.TopDamageLabel = new System.Windows.Forms.Label();
            this.InvolvedPartiesPanel = new System.Windows.Forms.Panel();
            this.FinalBlowAttacker = new EVEMon.Controls.KillReportAttacker();
            this.TopDamageAttacker = new EVEMon.Controls.KillReportAttacker();
            this.MainTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainTableLayoutPanel
            // 
            this.MainTableLayoutPanel.AutoSize = true;
            this.MainTableLayoutPanel.ColumnCount = 1;
            this.MainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainTableLayoutPanel.Controls.Add(this.InvolvedPartiesLabel, 0, 0);
            this.MainTableLayoutPanel.Controls.Add(this.TotalDamageTakenLabel, 0, 1);
            this.MainTableLayoutPanel.Controls.Add(this.FinalBlowAttacker, 0, 3);
            this.MainTableLayoutPanel.Controls.Add(this.TopDamageAttacker, 0, 5);
            this.MainTableLayoutPanel.Controls.Add(this.FinalBlowLabel, 0, 2);
            this.MainTableLayoutPanel.Controls.Add(this.TopDamageLabel, 0, 4);
            this.MainTableLayoutPanel.Controls.Add(this.InvolvedPartiesPanel, 0, 6);
            this.MainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainTableLayoutPanel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.MainTableLayoutPanel.Location = new System.Drawing.Point(0, 3);
            this.MainTableLayoutPanel.Name = "MainTableLayoutPanel";
            this.MainTableLayoutPanel.RowCount = 7;
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainTableLayoutPanel.Size = new System.Drawing.Size(262, 401);
            this.MainTableLayoutPanel.TabIndex = 0;
            // 
            // InvolvedPartiesLabel
            // 
            this.InvolvedPartiesLabel.AutoSize = true;
            this.InvolvedPartiesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.InvolvedPartiesLabel.Location = new System.Drawing.Point(3, 0);
            this.InvolvedPartiesLabel.Name = "InvolvedPartiesLabel";
            this.InvolvedPartiesLabel.Size = new System.Drawing.Size(132, 16);
            this.InvolvedPartiesLabel.TabIndex = 0;
            this.InvolvedPartiesLabel.Text = "Involved Parties ({0})";
            // 
            // TotalDamageTakenLabel
            // 
            this.TotalDamageTakenLabel.AutoSize = true;
            this.TotalDamageTakenLabel.ForeColor = System.Drawing.Color.Red;
            this.TotalDamageTakenLabel.Location = new System.Drawing.Point(3, 16);
            this.TotalDamageTakenLabel.Name = "TotalDamageTakenLabel";
            this.TotalDamageTakenLabel.Size = new System.Drawing.Size(125, 13);
            this.TotalDamageTakenLabel.TabIndex = 1;
            this.TotalDamageTakenLabel.Text = "{0:N0} Total Damage Taken";
            // 
            // FinalBlowLabel
            // 
            this.FinalBlowLabel.AutoSize = true;
            this.FinalBlowLabel.Location = new System.Drawing.Point(3, 29);
            this.FinalBlowLabel.Name = "FinalBlowLabel";
            this.FinalBlowLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.FinalBlowLabel.Size = new System.Drawing.Size(55, 19);
            this.FinalBlowLabel.TabIndex = 4;
            this.FinalBlowLabel.Text = "Final Blow";
            // 
            // TopDamageLabel
            // 
            this.TopDamageLabel.AutoSize = true;
            this.TopDamageLabel.Location = new System.Drawing.Point(3, 118);
            this.TopDamageLabel.Name = "TopDamageLabel";
            this.TopDamageLabel.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.TopDamageLabel.Size = new System.Drawing.Size(67, 19);
            this.TopDamageLabel.TabIndex = 5;
            this.TopDamageLabel.Text = "Top damage";
            // 
            // InvolvedPartiesPanel
            // 
            this.InvolvedPartiesPanel.AutoScroll = true;
            this.InvolvedPartiesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InvolvedPartiesPanel.Location = new System.Drawing.Point(0, 207);
            this.InvolvedPartiesPanel.Margin = new System.Windows.Forms.Padding(0);
            this.InvolvedPartiesPanel.Name = "InvolvedPartiesPanel";
            this.InvolvedPartiesPanel.Size = new System.Drawing.Size(262, 194);
            this.InvolvedPartiesPanel.TabIndex = 6;
            // 
            // FinalBlowAttacker
            // 
            this.FinalBlowAttacker.AutoSize = true;
            this.FinalBlowAttacker.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FinalBlowAttacker.Location = new System.Drawing.Point(3, 48);
            this.FinalBlowAttacker.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FinalBlowAttacker.Name = "FinalBlowAttacker";
            this.FinalBlowAttacker.Size = new System.Drawing.Size(256, 70);
            this.FinalBlowAttacker.TabIndex = 2;
            // 
            // TopDamageAttacker
            // 
            this.TopDamageAttacker.AutoSize = true;
            this.TopDamageAttacker.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TopDamageAttacker.Location = new System.Drawing.Point(3, 137);
            this.TopDamageAttacker.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.TopDamageAttacker.Name = "TopDamageAttacker";
            this.TopDamageAttacker.Size = new System.Drawing.Size(256, 70);
            this.TopDamageAttacker.TabIndex = 3;
            // 
            // KillReportInvolvedParties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.MainTableLayoutPanel);
            this.Name = "KillReportInvolvedParties";
            this.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.Size = new System.Drawing.Size(262, 404);
            this.MainTableLayoutPanel.ResumeLayout(false);
            this.MainTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel MainTableLayoutPanel;
        private System.Windows.Forms.Label InvolvedPartiesLabel;
        private System.Windows.Forms.Label TotalDamageTakenLabel;
        private KillReportAttacker FinalBlowAttacker;
        private KillReportAttacker TopDamageAttacker;
        private System.Windows.Forms.Label FinalBlowLabel;
        private System.Windows.Forms.Label TopDamageLabel;
        private System.Windows.Forms.Panel InvolvedPartiesPanel;
    }
}
