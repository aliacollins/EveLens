// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.NotificationWindow
{
    partial class OwnedSkillBooksWindow
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
            System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Not Known", System.Windows.Forms.HorizontalAlignment.Left);
            System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("Already Known", System.Windows.Forms.HorizontalAlignment.Left);
            this.noOwnedSkillbooksLabel = new System.Windows.Forms.Label();
            this.lvOwnedSkillBooks = new System.Windows.Forms.ListView();
            this.chSkill = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chPrereq = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // noOwnedSkillbooksLabel
            // 
            this.noOwnedSkillbooksLabel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.noOwnedSkillbooksLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.noOwnedSkillbooksLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.noOwnedSkillbooksLabel.Location = new System.Drawing.Point(0, 0);
            this.noOwnedSkillbooksLabel.Margin = new System.Windows.Forms.Padding(0);
            this.noOwnedSkillbooksLabel.Name = "noOwnedSkillbooksLabel";
            this.noOwnedSkillbooksLabel.Size = new System.Drawing.Size(394, 352);
            this.noOwnedSkillbooksLabel.TabIndex = 5;
            this.noOwnedSkillbooksLabel.Text = "No Skill books owned.";
            this.noOwnedSkillbooksLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lvOwnedSkillBooks
            // 
            this.lvOwnedSkillBooks.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lvOwnedSkillBooks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chSkill,
            this.chPrereq});
            this.lvOwnedSkillBooks.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvOwnedSkillBooks.FullRowSelect = true;
            listViewGroup1.Header = "Not Known";
            listViewGroup1.Name = "listViewGroupNotKnown";
            listViewGroup2.Header = "Already Known";
            listViewGroup2.Name = "listViewGroupKnown";
            this.lvOwnedSkillBooks.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1,
            listViewGroup2});
            this.lvOwnedSkillBooks.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvOwnedSkillBooks.HideSelection = false;
            this.lvOwnedSkillBooks.Location = new System.Drawing.Point(0, 0);
            this.lvOwnedSkillBooks.MultiSelect = false;
            this.lvOwnedSkillBooks.Name = "lvOwnedSkillBooks";
            this.lvOwnedSkillBooks.Size = new System.Drawing.Size(394, 352);
            this.lvOwnedSkillBooks.TabIndex = 6;
            this.lvOwnedSkillBooks.UseCompatibleStateImageBehavior = false;
            this.lvOwnedSkillBooks.View = System.Windows.Forms.View.Details;
            this.lvOwnedSkillBooks.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.lvOwnedSkillBooks_ColumnWidthChanging);
            // 
            // chSkill
            // 
            this.chSkill.Text = "Skill Book Name";
            this.chSkill.Width = 114;
            // 
            // chPrereq
            // 
            this.chPrereq.Text = "Prerequisites Met";
            this.chPrereq.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.chPrereq.Width = 106;
            // 
            // OwnedSkillBooksWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(394, 352);
            this.Controls.Add(this.lvOwnedSkillBooks);
            this.Controls.Add(this.noOwnedSkillbooksLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "OwnedSkillBooksWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Skill books owned by {0}";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label noOwnedSkillbooksLabel;
        private System.Windows.Forms.ListView lvOwnedSkillBooks;
        private System.Windows.Forms.ColumnHeader chSkill;
        private System.Windows.Forms.ColumnHeader chPrereq;
    }
}