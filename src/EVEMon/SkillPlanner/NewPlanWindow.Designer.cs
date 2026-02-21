// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.SkillPlanner
{
    partial class NewPlanWindow
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
            this.PlanNameLabel = new System.Windows.Forms.Label();
            this.PlanNameTextBox = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.PlanDescriptionTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // PlanNameLabel
            // 
            this.PlanNameLabel.AutoSize = true;
            this.PlanNameLabel.Location = new System.Drawing.Point(15, 13);
            this.PlanNameLabel.Name = "PlanNameLabel";
            this.PlanNameLabel.Size = new System.Drawing.Size(135, 13);
            this.PlanNameLabel.TabIndex = 0;
            this.PlanNameLabel.Text = "Enter a name for this plan:";
            // 
            // PlanNameTextBox
            // 
            this.PlanNameTextBox.Location = new System.Drawing.Point(16, 30);
            this.PlanNameTextBox.Name = "PlanNameTextBox";
            this.PlanNameTextBox.Size = new System.Drawing.Size(264, 21);
            this.PlanNameTextBox.TabIndex = 1;
            this.PlanNameTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Enabled = false;
            this.btnOk.Location = new System.Drawing.Point(124, 159);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(205, 159);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // PlanDescriptionTextBox
            // 
            this.PlanDescriptionTextBox.AcceptsReturn = true;
            this.PlanDescriptionTextBox.Location = new System.Drawing.Point(16, 79);
            this.PlanDescriptionTextBox.MaxLength = 255;
            this.PlanDescriptionTextBox.Multiline = true;
            this.PlanDescriptionTextBox.Name = "PlanDescriptionTextBox";
            this.PlanDescriptionTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.PlanDescriptionTextBox.Size = new System.Drawing.Size(264, 72);
            this.PlanDescriptionTextBox.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(210, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Enter a description for this plan (optional):";
            // 
            // NewPlanWindow
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(292, 194);
            this.Controls.Add(this.PlanDescriptionTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.PlanNameTextBox);
            this.Controls.Add(this.PlanNameLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewPlanWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "New Plan";
            this.Shown += new System.EventHandler(this.NewPlanWindow_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label PlanNameLabel;
        private System.Windows.Forms.TextBox PlanNameTextBox;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox PlanDescriptionTextBox;
        private System.Windows.Forms.Label label2;
    }
}