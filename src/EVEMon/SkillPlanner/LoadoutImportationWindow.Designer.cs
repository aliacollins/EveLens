// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Controls;

namespace EVEMon.SkillPlanner
{
    partial class LoadoutImportationWindow
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
            this.components = new System.ComponentModel.Container();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ShowInBrowserMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showInMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.cmiExpandSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiCollapseSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.selectedSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.cmiExpandAll = new System.Windows.Forms.ToolStripMenuItem();
            this.cmiCollapseAll = new System.Windows.Forms.ToolStripMenuItem();
            this.BottomPanel = new System.Windows.Forms.Panel();
            this.TrainTimeLabel = new System.Windows.Forms.Label();
            this.PlanedLabel = new System.Windows.Forms.Label();
            this.AddToPlanButton = new System.Windows.Forms.Button();
            this.TrainingTimeLabel = new System.Windows.Forms.Label();
            this.CloseButton = new System.Windows.Forms.Button();
            this.ExplanationLabel = new System.Windows.Forms.Label();
            this.ResultsTreeView = new System.Windows.Forms.TreeView();
            this.HeaderPanel = new System.Windows.Forms.Panel();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.ShipTypeNameLabel = new System.Windows.Forms.Label();
            this.LoadoutNameLabel = new System.Windows.Forms.Label();
            this.TreeViewPanel = new System.Windows.Forms.Panel();
            this.contextMenu.SuspendLayout();
            this.BottomPanel.SuspendLayout();
            this.HeaderPanel.SuspendLayout();
            this.TreeViewPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ShowInBrowserMenuItem,
            this.showInMenuSeparator,
            this.cmiExpandSelected,
            this.cmiCollapseSelected,
            this.selectedSeparator,
            this.cmiExpandAll,
            this.cmiCollapseAll});
            this.contextMenu.Name = "cmNode";
            this.contextMenu.Size = new System.Drawing.Size(198, 126);
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
            // 
            // ShowInBrowserMenuItem
            // 
            this.ShowInBrowserMenuItem.Name = "ShowInBrowserMenuItem";
            this.ShowInBrowserMenuItem.Size = new System.Drawing.Size(197, 22);
            this.ShowInBrowserMenuItem.Text = "Show in &Item Browser...";
            this.ShowInBrowserMenuItem.Click += new System.EventHandler(this.tvLoadout_DoubleClick);
            // 
            // showInMenuSeparator
            // 
            this.showInMenuSeparator.Name = "showInMenuSeparator";
            this.showInMenuSeparator.Size = new System.Drawing.Size(194, 6);
            // 
            // cmiExpandSelected
            // 
            this.cmiExpandSelected.Name = "cmiExpandSelected";
            this.cmiExpandSelected.Size = new System.Drawing.Size(197, 22);
            this.cmiExpandSelected.Text = "Expand Selected";
            this.cmiExpandSelected.Click += new System.EventHandler(this.cmiExpandSelected_Click);
            // 
            // cmiCollapseSelected
            // 
            this.cmiCollapseSelected.Name = "cmiCollapseSelected";
            this.cmiCollapseSelected.Size = new System.Drawing.Size(197, 22);
            this.cmiCollapseSelected.Text = "Collapse Selected";
            this.cmiCollapseSelected.Click += new System.EventHandler(this.cmiCollapseSelected_Click);
            // 
            // selectedSeparator
            // 
            this.selectedSeparator.Name = "selectedSeparator";
            this.selectedSeparator.Size = new System.Drawing.Size(194, 6);
            // 
            // cmiExpandAll
            // 
            this.cmiExpandAll.Name = "cmiExpandAll";
            this.cmiExpandAll.Size = new System.Drawing.Size(197, 22);
            this.cmiExpandAll.Text = "&Expand All";
            this.cmiExpandAll.Click += new System.EventHandler(this.cmiExpandAll_Click);
            // 
            // cmiCollapseAll
            // 
            this.cmiCollapseAll.Name = "cmiCollapseAll";
            this.cmiCollapseAll.Size = new System.Drawing.Size(197, 22);
            this.cmiCollapseAll.Text = "&Collapse All";
            this.cmiCollapseAll.Click += new System.EventHandler(this.cmiCollapseAll_Click);
            // 
            // BottomPanel
            // 
            this.BottomPanel.Controls.Add(this.TrainTimeLabel);
            this.BottomPanel.Controls.Add(this.PlanedLabel);
            this.BottomPanel.Controls.Add(this.AddToPlanButton);
            this.BottomPanel.Controls.Add(this.TrainingTimeLabel);
            this.BottomPanel.Controls.Add(this.CloseButton);
            this.BottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.BottomPanel.Location = new System.Drawing.Point(0, 370);
            this.BottomPanel.Name = "BottomPanel";
            this.BottomPanel.Size = new System.Drawing.Size(374, 72);
            this.BottomPanel.TabIndex = 2;
            // 
            // TrainTimeLabel
            // 
            this.TrainTimeLabel.AutoSize = true;
            this.TrainTimeLabel.Location = new System.Drawing.Point(191, 14);
            this.TrainTimeLabel.Name = "TrainTimeLabel";
            this.TrainTimeLabel.Size = new System.Drawing.Size(27, 13);
            this.TrainTimeLabel.TabIndex = 32;
            this.TrainTimeLabel.Text = "N/A";
            // 
            // PlanedLabel
            // 
            this.PlanedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PlanedLabel.AutoSize = true;
            this.PlanedLabel.Location = new System.Drawing.Point(191, 14);
            this.PlanedLabel.Name = "PlanedLabel";
            this.PlanedLabel.Size = new System.Drawing.Size(0, 13);
            this.PlanedLabel.TabIndex = 34;
            // 
            // AddToPlanButton
            // 
            this.AddToPlanButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AddToPlanButton.Enabled = false;
            this.AddToPlanButton.Location = new System.Drawing.Point(206, 37);
            this.AddToPlanButton.Name = "AddToPlanButton";
            this.AddToPlanButton.Size = new System.Drawing.Size(75, 23);
            this.AddToPlanButton.TabIndex = 2;
            this.AddToPlanButton.Text = "Add To Plan";
            this.AddToPlanButton.UseVisualStyleBackColor = true;
            this.AddToPlanButton.Click += new System.EventHandler(this.btnPlan_Click);
            // 
            // TrainingTimeLabel
            // 
            this.TrainingTimeLabel.AutoSize = true;
            this.TrainingTimeLabel.Location = new System.Drawing.Point(12, 14);
            this.TrainingTimeLabel.Name = "TrainingTimeLabel";
            this.TrainingTimeLabel.Size = new System.Drawing.Size(173, 13);
            this.TrainingTimeLabel.TabIndex = 31;
            this.TrainingTimeLabel.Text = "Training Time for selected loadout: ";
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseButton.Location = new System.Drawing.Point(287, 37);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 3;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ExplanationLabel
            // 
            this.ExplanationLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.ExplanationLabel.Location = new System.Drawing.Point(0, 0);
            this.ExplanationLabel.Name = "ExplanationLabel";
            this.ExplanationLabel.Padding = new System.Windows.Forms.Padding(2, 5, 2, 5);
            this.ExplanationLabel.Size = new System.Drawing.Size(374, 40);
            this.ExplanationLabel.TabIndex = 1;
            this.ExplanationLabel.Text = "Copy an EFT, XML, CLF or DNA formated loadout into the clipboard.";
            this.ExplanationLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // ResultsTreeView
            // 
            this.ResultsTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ResultsTreeView.Location = new System.Drawing.Point(5, 5);
            this.ResultsTreeView.Name = "ResultsTreeView";
            this.ResultsTreeView.Size = new System.Drawing.Size(364, 286);
            this.ResultsTreeView.TabIndex = 1;
            this.ResultsTreeView.DoubleClick += new System.EventHandler(this.tvLoadout_DoubleClick);
            this.ResultsTreeView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.tvLoadout_MouseMove);
            this.ResultsTreeView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.tvLoadout_MouseUp);
            // 
            // HeaderPanel
            // 
            this.HeaderPanel.AutoSize = true;
            this.HeaderPanel.Controls.Add(this.DescriptionLabel);
            this.HeaderPanel.Controls.Add(this.ShipTypeNameLabel);
            this.HeaderPanel.Controls.Add(this.LoadoutNameLabel);
            this.HeaderPanel.Controls.Add(this.ExplanationLabel);
            this.HeaderPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.HeaderPanel.Location = new System.Drawing.Point(0, 0);
            this.HeaderPanel.Name = "HeaderPanel";
            this.HeaderPanel.Size = new System.Drawing.Size(374, 74);
            this.HeaderPanel.TabIndex = 3;
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.AutoSize = true;
            this.DescriptionLabel.Location = new System.Drawing.Point(12, 61);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(63, 13);
            this.DescriptionLabel.TabIndex = 4;
            this.DescriptionLabel.Text = "Description:";
            // 
            // ShipTypeNameLabel
            // 
            this.ShipTypeNameLabel.AutoSize = true;
            this.ShipTypeNameLabel.Location = new System.Drawing.Point(12, 35);
            this.ShipTypeNameLabel.Name = "ShipTypeNameLabel";
            this.ShipTypeNameLabel.Size = new System.Drawing.Size(31, 13);
            this.ShipTypeNameLabel.TabIndex = 3;
            this.ShipTypeNameLabel.Text = "Ship:";
            // 
            // LoadoutNameLabel
            // 
            this.LoadoutNameLabel.AutoSize = true;
            this.LoadoutNameLabel.Location = new System.Drawing.Point(12, 48);
            this.LoadoutNameLabel.Name = "LoadoutNameLabel";
            this.LoadoutNameLabel.Size = new System.Drawing.Size(38, 13);
            this.LoadoutNameLabel.TabIndex = 2;
            this.LoadoutNameLabel.Text = "Name:";
            // 
            // TreeViewPanel
            // 
            this.TreeViewPanel.Controls.Add(this.ResultsTreeView);
            this.TreeViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeViewPanel.Location = new System.Drawing.Point(0, 74);
            this.TreeViewPanel.Name = "TreeViewPanel";
            this.TreeViewPanel.Padding = new System.Windows.Forms.Padding(5);
            this.TreeViewPanel.Size = new System.Drawing.Size(374, 296);
            this.TreeViewPanel.TabIndex = 4;
            // 
            // LoadoutImportationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(374, 442);
            this.Controls.Add(this.TreeViewPanel);
            this.Controls.Add(this.HeaderPanel);
            this.Controls.Add(this.BottomPanel);
            this.MaximumSize = new System.Drawing.Size(390, 1280);
            this.MinimumSize = new System.Drawing.Size(390, 480);
            this.Name = "LoadoutImportationForm";
            this.Text = "Loadout Import";
            this.contextMenu.ResumeLayout(false);
            this.BottomPanel.ResumeLayout(false);
            this.BottomPanel.PerformLayout();
            this.HeaderPanel.ResumeLayout(false);
            this.HeaderPanel.PerformLayout();
            this.TreeViewPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView ResultsTreeView;
        private System.Windows.Forms.Panel BottomPanel;
        private System.Windows.Forms.Button AddToPlanButton;
        private System.Windows.Forms.Label TrainingTimeLabel;
        private System.Windows.Forms.Label TrainTimeLabel;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Label PlanedLabel;
        private System.Windows.Forms.Label ExplanationLabel;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem ShowInBrowserMenuItem;
        private System.Windows.Forms.Panel HeaderPanel;
        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.Label ShipTypeNameLabel;
        private System.Windows.Forms.Label LoadoutNameLabel;
        private System.Windows.Forms.Panel TreeViewPanel;
        private System.Windows.Forms.ToolStripSeparator showInMenuSeparator;
        private System.Windows.Forms.ToolStripMenuItem cmiExpandAll;
        private System.Windows.Forms.ToolStripMenuItem cmiCollapseAll;
        private System.Windows.Forms.ToolStripMenuItem cmiExpandSelected;
        private System.Windows.Forms.ToolStripMenuItem cmiCollapseSelected;
        private System.Windows.Forms.ToolStripSeparator selectedSeparator;
    }
}
