namespace EVEMon.SkillPlanner
{
    partial class ImplantCalculatorWindow
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
            this.gbAttributes = new System.Windows.Forms.GroupBox();
            this.AtrributesPanel = new System.Windows.Forms.Panel();
            this.lblEffectiveWillpower = new System.Windows.Forms.Label();
            this.nudIntelligence = new System.Windows.Forms.NumericUpDown();
            this.lblEffectiveMemory = new System.Windows.Forms.Label();
            this.lblIntelligence = new System.Windows.Forms.Label();
            this.lblEffectivePerception = new System.Windows.Forms.Label();
            this.nudCharisma = new System.Windows.Forms.NumericUpDown();
            this.nudPerception = new System.Windows.Forms.NumericUpDown();
            this.lblMemory = new System.Windows.Forms.Label();
            this.lblCharisma = new System.Windows.Forms.Label();
            this.lblAdjustMemory = new System.Windows.Forms.Label();
            this.lblEffectiveCharisma = new System.Windows.Forms.Label();
            this.nudMemory = new System.Windows.Forms.NumericUpDown();
            this.lblEffectiveIntelligence = new System.Windows.Forms.Label();
            this.lbWillpower = new System.Windows.Forms.Label();
            this.lblAdjustCharisma = new System.Windows.Forms.Label();
            this.lblEffective = new System.Windows.Forms.Label();
            this.lblAdjustWillpower = new System.Windows.Forms.Label();
            this.lblPerception = new System.Windows.Forms.Label();
            this.nudWillpower = new System.Windows.Forms.NumericUpDown();
            this.lblAdjustPerception = new System.Windows.Forms.Label();
            this.lblAdjustIntelligence = new System.Windows.Forms.Label();
            this.lblImplants = new System.Windows.Forms.Label();
            this.gbCurrentBase = new System.Windows.Forms.GroupBox();
            this.lblBaseDate = new System.Windows.Forms.Label();
            this.lblBaseSpan = new System.Windows.Forms.Label();
            this.gbCurrent = new System.Windows.Forms.GroupBox();
            this.lblCurrentDate = new System.Windows.Forms.Label();
            this.lblCurrentSpan = new System.Windows.Forms.Label();
            this.gbWithThisAttributes = new System.Windows.Forms.GroupBox();
            this.lblComparedToCurrent = new System.Windows.Forms.Label();
            this.lblComparedToBase = new System.Windows.Forms.Label();
            this.lblThisDate = new System.Windows.Forms.Label();
            this.lblThisSpan = new System.Windows.Forms.Label();
            this.miNoImplants = new System.Windows.Forms.ToolStripMenuItem();
            this.miCurrentClone = new System.Windows.Forms.ToolStripMenuItem();
            this.miJumpClone1 = new System.Windows.Forms.ToolStripMenuItem();
            this.miJumpClone2 = new System.Windows.Forms.ToolStripMenuItem();
            this.miJumpClone3 = new System.Windows.Forms.ToolStripMenuItem();
            this.miJumpClone4 = new System.Windows.Forms.ToolStripMenuItem();
            this.miJumpClone5 = new System.Windows.Forms.ToolStripMenuItem();
            this.lblNotice = new System.Windows.Forms.Label();
            this.gbAttributes.SuspendLayout();
            this.AtrributesPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIntelligence)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCharisma)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPerception)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMemory)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudWillpower)).BeginInit();
            this.gbCurrentBase.SuspendLayout();
            this.gbCurrent.SuspendLayout();
            this.gbWithThisAttributes.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbAttributes
            // 
            this.gbAttributes.Controls.Add(this.AtrributesPanel);
            this.gbAttributes.Location = new System.Drawing.Point(9, 25);
            this.gbAttributes.Name = "gbAttributes";
            this.gbAttributes.Size = new System.Drawing.Size(249, 188);
            this.gbAttributes.TabIndex = 0;
            this.gbAttributes.TabStop = false;
            this.gbAttributes.Text = "Attributes of \"Implant Set Name\"";
            // 
            // AtrributesPanel
            // 
            this.AtrributesPanel.Controls.Add(this.lblEffectiveWillpower);
            this.AtrributesPanel.Controls.Add(this.nudIntelligence);
            this.AtrributesPanel.Controls.Add(this.lblEffectiveMemory);
            this.AtrributesPanel.Controls.Add(this.lblIntelligence);
            this.AtrributesPanel.Controls.Add(this.lblEffectivePerception);
            this.AtrributesPanel.Controls.Add(this.nudCharisma);
            this.AtrributesPanel.Controls.Add(this.nudPerception);
            this.AtrributesPanel.Controls.Add(this.lblMemory);
            this.AtrributesPanel.Controls.Add(this.lblCharisma);
            this.AtrributesPanel.Controls.Add(this.lblAdjustMemory);
            this.AtrributesPanel.Controls.Add(this.lblEffectiveCharisma);
            this.AtrributesPanel.Controls.Add(this.nudMemory);
            this.AtrributesPanel.Controls.Add(this.lblEffectiveIntelligence);
            this.AtrributesPanel.Controls.Add(this.lbWillpower);
            this.AtrributesPanel.Controls.Add(this.lblAdjustCharisma);
            this.AtrributesPanel.Controls.Add(this.lblEffective);
            this.AtrributesPanel.Controls.Add(this.lblAdjustWillpower);
            this.AtrributesPanel.Controls.Add(this.lblPerception);
            this.AtrributesPanel.Controls.Add(this.nudWillpower);
            this.AtrributesPanel.Controls.Add(this.lblAdjustPerception);
            this.AtrributesPanel.Controls.Add(this.lblAdjustIntelligence);
            this.AtrributesPanel.Controls.Add(this.lblImplants);
            this.AtrributesPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.AtrributesPanel.Location = new System.Drawing.Point(3, 28);
            this.AtrributesPanel.Name = "AtrributesPanel";
            this.AtrributesPanel.Padding = new System.Windows.Forms.Padding(3);
            this.AtrributesPanel.Size = new System.Drawing.Size(243, 157);
            this.AtrributesPanel.TabIndex = 5;
            // 
            // lblEffectiveWillpower
            // 
            this.lblEffectiveWillpower.AutoSize = true;
            this.lblEffectiveWillpower.Location = new System.Drawing.Point(177, 110);
            this.lblEffectiveWillpower.Name = "lblEffectiveWillpower";
            this.lblEffectiveWillpower.Size = new System.Drawing.Size(34, 13);
            this.lblEffectiveWillpower.TabIndex = 17;
            this.lblEffectiveWillpower.Text = "22.14";
            // 
            // nudIntelligence
            // 
            this.nudIntelligence.Location = new System.Drawing.Point(71, 24);
            this.nudIntelligence.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudIntelligence.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudIntelligence.Name = "nudIntelligence";
            this.nudIntelligence.Size = new System.Drawing.Size(39, 20);
            this.nudIntelligence.TabIndex = 3;
            this.nudIntelligence.Tag = "0";
            this.nudIntelligence.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudIntelligence.ValueChanged += new System.EventHandler(this.nudIntelligence_ValueChanged);
            // 
            // lblEffectiveMemory
            // 
            this.lblEffectiveMemory.AutoSize = true;
            this.lblEffectiveMemory.Location = new System.Drawing.Point(177, 137);
            this.lblEffectiveMemory.Name = "lblEffectiveMemory";
            this.lblEffectiveMemory.Size = new System.Drawing.Size(34, 13);
            this.lblEffectiveMemory.TabIndex = 21;
            this.lblEffectiveMemory.Text = "22.14";
            // 
            // lblIntelligence
            // 
            this.lblIntelligence.AutoSize = true;
            this.lblIntelligence.Location = new System.Drawing.Point(2, 29);
            this.lblIntelligence.Name = "lblIntelligence";
            this.lblIntelligence.Size = new System.Drawing.Size(64, 13);
            this.lblIntelligence.TabIndex = 2;
            this.lblIntelligence.Text = "Intelligence:";
            // 
            // lblEffectivePerception
            // 
            this.lblEffectivePerception.AutoSize = true;
            this.lblEffectivePerception.Location = new System.Drawing.Point(177, 56);
            this.lblEffectivePerception.Name = "lblEffectivePerception";
            this.lblEffectivePerception.Size = new System.Drawing.Size(34, 13);
            this.lblEffectivePerception.TabIndex = 9;
            this.lblEffectivePerception.Text = "22.14";
            // 
            // nudCharisma
            // 
            this.nudCharisma.Location = new System.Drawing.Point(71, 78);
            this.nudCharisma.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudCharisma.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudCharisma.Name = "nudCharisma";
            this.nudCharisma.Size = new System.Drawing.Size(39, 20);
            this.nudCharisma.TabIndex = 11;
            this.nudCharisma.Tag = "2";
            this.nudCharisma.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudCharisma.ValueChanged += new System.EventHandler(this.nudCharisma_ValueChanged);
            // 
            // nudPerception
            // 
            this.nudPerception.Location = new System.Drawing.Point(71, 51);
            this.nudPerception.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudPerception.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudPerception.Name = "nudPerception";
            this.nudPerception.Size = new System.Drawing.Size(39, 20);
            this.nudPerception.TabIndex = 7;
            this.nudPerception.Tag = "1";
            this.nudPerception.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudPerception.ValueChanged += new System.EventHandler(this.nudPerception_ValueChanged);
            // 
            // lblMemory
            // 
            this.lblMemory.AutoSize = true;
            this.lblMemory.Location = new System.Drawing.Point(2, 137);
            this.lblMemory.Name = "lblMemory";
            this.lblMemory.Size = new System.Drawing.Size(47, 13);
            this.lblMemory.TabIndex = 18;
            this.lblMemory.Text = "Memory:";
            // 
            // lblCharisma
            // 
            this.lblCharisma.AutoSize = true;
            this.lblCharisma.Location = new System.Drawing.Point(2, 83);
            this.lblCharisma.Name = "lblCharisma";
            this.lblCharisma.Size = new System.Drawing.Size(53, 13);
            this.lblCharisma.TabIndex = 10;
            this.lblCharisma.Text = "Charisma:";
            // 
            // lblAdjustMemory
            // 
            this.lblAdjustMemory.AutoSize = true;
            this.lblAdjustMemory.Location = new System.Drawing.Point(131, 137);
            this.lblAdjustMemory.Name = "lblAdjustMemory";
            this.lblAdjustMemory.Size = new System.Drawing.Size(19, 13);
            this.lblAdjustMemory.TabIndex = 20;
            this.lblAdjustMemory.Text = "+0";
            // 
            // lblEffectiveCharisma
            // 
            this.lblEffectiveCharisma.AutoSize = true;
            this.lblEffectiveCharisma.Location = new System.Drawing.Point(177, 83);
            this.lblEffectiveCharisma.Name = "lblEffectiveCharisma";
            this.lblEffectiveCharisma.Size = new System.Drawing.Size(34, 13);
            this.lblEffectiveCharisma.TabIndex = 13;
            this.lblEffectiveCharisma.Text = "22.14";
            // 
            // nudMemory
            // 
            this.nudMemory.Location = new System.Drawing.Point(71, 132);
            this.nudMemory.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudMemory.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudMemory.Name = "nudMemory";
            this.nudMemory.Size = new System.Drawing.Size(39, 20);
            this.nudMemory.TabIndex = 19;
            this.nudMemory.Tag = "4";
            this.nudMemory.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudMemory.ValueChanged += new System.EventHandler(this.nudMemory_ValueChanged);
            // 
            // lblEffectiveIntelligence
            // 
            this.lblEffectiveIntelligence.AutoSize = true;
            this.lblEffectiveIntelligence.Location = new System.Drawing.Point(177, 29);
            this.lblEffectiveIntelligence.Name = "lblEffectiveIntelligence";
            this.lblEffectiveIntelligence.Size = new System.Drawing.Size(34, 13);
            this.lblEffectiveIntelligence.TabIndex = 5;
            this.lblEffectiveIntelligence.Text = "22.14";
            // 
            // lbWillpower
            // 
            this.lbWillpower.AutoSize = true;
            this.lbWillpower.Location = new System.Drawing.Point(2, 110);
            this.lbWillpower.Name = "lbWillpower";
            this.lbWillpower.Size = new System.Drawing.Size(56, 13);
            this.lbWillpower.TabIndex = 14;
            this.lbWillpower.Text = "Willpower:";
            // 
            // lblAdjustCharisma
            // 
            this.lblAdjustCharisma.AutoSize = true;
            this.lblAdjustCharisma.Location = new System.Drawing.Point(131, 83);
            this.lblAdjustCharisma.Name = "lblAdjustCharisma";
            this.lblAdjustCharisma.Size = new System.Drawing.Size(19, 13);
            this.lblAdjustCharisma.TabIndex = 12;
            this.lblAdjustCharisma.Text = "+0";
            // 
            // lblEffective
            // 
            this.lblEffective.AutoSize = true;
            this.lblEffective.Location = new System.Drawing.Point(171, 5);
            this.lblEffective.Name = "lblEffective";
            this.lblEffective.Size = new System.Drawing.Size(49, 13);
            this.lblEffective.TabIndex = 1;
            this.lblEffective.Text = "Effective";
            // 
            // lblAdjustWillpower
            // 
            this.lblAdjustWillpower.AutoSize = true;
            this.lblAdjustWillpower.Location = new System.Drawing.Point(131, 110);
            this.lblAdjustWillpower.Name = "lblAdjustWillpower";
            this.lblAdjustWillpower.Size = new System.Drawing.Size(19, 13);
            this.lblAdjustWillpower.TabIndex = 16;
            this.lblAdjustWillpower.Text = "+0";
            // 
            // lblPerception
            // 
            this.lblPerception.AutoSize = true;
            this.lblPerception.Location = new System.Drawing.Point(2, 56);
            this.lblPerception.Name = "lblPerception";
            this.lblPerception.Size = new System.Drawing.Size(61, 13);
            this.lblPerception.TabIndex = 6;
            this.lblPerception.Text = "Perception:";
            // 
            // nudWillpower
            // 
            this.nudWillpower.Location = new System.Drawing.Point(71, 105);
            this.nudWillpower.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudWillpower.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudWillpower.Name = "nudWillpower";
            this.nudWillpower.Size = new System.Drawing.Size(39, 20);
            this.nudWillpower.TabIndex = 15;
            this.nudWillpower.Tag = "3";
            this.nudWillpower.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudWillpower.ValueChanged += new System.EventHandler(this.nudWillpower_ValueChanged);
            // 
            // lblAdjustPerception
            // 
            this.lblAdjustPerception.AutoSize = true;
            this.lblAdjustPerception.Location = new System.Drawing.Point(131, 56);
            this.lblAdjustPerception.Name = "lblAdjustPerception";
            this.lblAdjustPerception.Size = new System.Drawing.Size(19, 13);
            this.lblAdjustPerception.TabIndex = 8;
            this.lblAdjustPerception.Text = "+0";
            // 
            // lblAdjustIntelligence
            // 
            this.lblAdjustIntelligence.AutoSize = true;
            this.lblAdjustIntelligence.Location = new System.Drawing.Point(131, 29);
            this.lblAdjustIntelligence.Name = "lblAdjustIntelligence";
            this.lblAdjustIntelligence.Size = new System.Drawing.Size(19, 13);
            this.lblAdjustIntelligence.TabIndex = 4;
            this.lblAdjustIntelligence.Text = "+0";
            // 
            // lblImplants
            // 
            this.lblImplants.AutoSize = true;
            this.lblImplants.Location = new System.Drawing.Point(120, 5);
            this.lblImplants.Name = "lblImplants";
            this.lblImplants.Size = new System.Drawing.Size(46, 13);
            this.lblImplants.TabIndex = 0;
            this.lblImplants.Text = "Implants";
            // 
            // gbCurrentBase
            // 
            this.gbCurrentBase.Controls.Add(this.lblBaseDate);
            this.gbCurrentBase.Controls.Add(this.lblBaseSpan);
            this.gbCurrentBase.Location = new System.Drawing.Point(264, 25);
            this.gbCurrentBase.Name = "gbCurrentBase";
            this.gbCurrentBase.Size = new System.Drawing.Size(279, 50);
            this.gbCurrentBase.TabIndex = 2;
            this.gbCurrentBase.TabStop = false;
            this.gbCurrentBase.Text = "Current base (without implants)";
            // 
            // lblBaseDate
            // 
            this.lblBaseDate.AutoSize = true;
            this.lblBaseDate.Location = new System.Drawing.Point(6, 30);
            this.lblBaseDate.Name = "lblBaseDate";
            this.lblBaseDate.Size = new System.Drawing.Size(94, 13);
            this.lblBaseDate.TabIndex = 1;
            this.lblBaseDate.Text = "Current Base Date";
            // 
            // lblBaseSpan
            // 
            this.lblBaseSpan.AutoSize = true;
            this.lblBaseSpan.Location = new System.Drawing.Point(6, 17);
            this.lblBaseSpan.Name = "lblBaseSpan";
            this.lblBaseSpan.Size = new System.Drawing.Size(96, 13);
            this.lblBaseSpan.TabIndex = 0;
            this.lblBaseSpan.Text = "Current Base Span";
            // 
            // gbCurrent
            // 
            this.gbCurrent.Controls.Add(this.lblCurrentDate);
            this.gbCurrent.Controls.Add(this.lblCurrentSpan);
            this.gbCurrent.Location = new System.Drawing.Point(264, 81);
            this.gbCurrent.Name = "gbCurrent";
            this.gbCurrent.Size = new System.Drawing.Size(279, 50);
            this.gbCurrent.TabIndex = 3;
            this.gbCurrent.TabStop = false;
            this.gbCurrent.Text = "Current (with implants)";
            // 
            // lblCurrentDate
            // 
            this.lblCurrentDate.AutoSize = true;
            this.lblCurrentDate.Location = new System.Drawing.Point(6, 30);
            this.lblCurrentDate.Name = "lblCurrentDate";
            this.lblCurrentDate.Size = new System.Drawing.Size(67, 13);
            this.lblCurrentDate.TabIndex = 1;
            this.lblCurrentDate.Text = "Current Date";
            // 
            // lblCurrentSpan
            // 
            this.lblCurrentSpan.AutoSize = true;
            this.lblCurrentSpan.Location = new System.Drawing.Point(6, 17);
            this.lblCurrentSpan.Name = "lblCurrentSpan";
            this.lblCurrentSpan.Size = new System.Drawing.Size(69, 13);
            this.lblCurrentSpan.TabIndex = 0;
            this.lblCurrentSpan.Text = "Current Span";
            // 
            // gbWithThisAttributes
            // 
            this.gbWithThisAttributes.Controls.Add(this.lblComparedToCurrent);
            this.gbWithThisAttributes.Controls.Add(this.lblComparedToBase);
            this.gbWithThisAttributes.Controls.Add(this.lblThisDate);
            this.gbWithThisAttributes.Controls.Add(this.lblThisSpan);
            this.gbWithThisAttributes.Location = new System.Drawing.Point(264, 137);
            this.gbWithThisAttributes.Name = "gbWithThisAttributes";
            this.gbWithThisAttributes.Size = new System.Drawing.Size(279, 76);
            this.gbWithThisAttributes.TabIndex = 4;
            this.gbWithThisAttributes.TabStop = false;
            this.gbWithThisAttributes.Text = "With These Attributes";
            // 
            // lblComparedToCurrent
            // 
            this.lblComparedToCurrent.AutoSize = true;
            this.lblComparedToCurrent.Location = new System.Drawing.Point(6, 56);
            this.lblComparedToCurrent.Name = "lblComparedToCurrent";
            this.lblComparedToCurrent.Size = new System.Drawing.Size(102, 13);
            this.lblComparedToCurrent.TabIndex = 3;
            this.lblComparedToCurrent.Text = "Compare To Current";
            // 
            // lblComparedToBase
            // 
            this.lblComparedToBase.AutoSize = true;
            this.lblComparedToBase.Location = new System.Drawing.Point(6, 43);
            this.lblComparedToBase.Name = "lblComparedToBase";
            this.lblComparedToBase.Size = new System.Drawing.Size(129, 13);
            this.lblComparedToBase.TabIndex = 2;
            this.lblComparedToBase.Text = "Compare To Current Base";
            // 
            // lblThisDate
            // 
            this.lblThisDate.AutoSize = true;
            this.lblThisDate.Location = new System.Drawing.Point(6, 30);
            this.lblThisDate.Name = "lblThisDate";
            this.lblThisDate.Size = new System.Drawing.Size(53, 13);
            this.lblThisDate.TabIndex = 1;
            this.lblThisDate.Text = "This Date";
            // 
            // lblThisSpan
            // 
            this.lblThisSpan.AutoSize = true;
            this.lblThisSpan.Location = new System.Drawing.Point(6, 17);
            this.lblThisSpan.Name = "lblThisSpan";
            this.lblThisSpan.Size = new System.Drawing.Size(55, 13);
            this.lblThisSpan.TabIndex = 0;
            this.lblThisSpan.Text = "This Span";
            // 
            // miNoImplants
            // 
            this.miNoImplants.Name = "miNoImplants";
            this.miNoImplants.Size = new System.Drawing.Size(32, 19);
            // 
            // miCurrentClone
            // 
            this.miCurrentClone.Name = "miCurrentClone";
            this.miCurrentClone.Size = new System.Drawing.Size(32, 19);
            // 
            // miJumpClone1
            // 
            this.miJumpClone1.Name = "miJumpClone1";
            this.miJumpClone1.Size = new System.Drawing.Size(32, 19);
            // 
            // miJumpClone2
            // 
            this.miJumpClone2.Name = "miJumpClone2";
            this.miJumpClone2.Size = new System.Drawing.Size(32, 19);
            // 
            // miJumpClone3
            // 
            this.miJumpClone3.Name = "miJumpClone3";
            this.miJumpClone3.Size = new System.Drawing.Size(32, 19);
            // 
            // miJumpClone4
            // 
            this.miJumpClone4.Name = "miJumpClone4";
            this.miJumpClone4.Size = new System.Drawing.Size(32, 19);
            // 
            // miJumpClone5
            // 
            this.miJumpClone5.Name = "miJumpClone5";
            this.miJumpClone5.Size = new System.Drawing.Size(32, 19);
            //
            // lblNotice
            //
            this.lblNotice.AutoSize = true;
            this.lblNotice.ForeColor = System.Drawing.Color.Red;
            this.lblNotice.Location = new System.Drawing.Point(6, 7);
            this.lblNotice.Name = "lblNotice";
            this.lblNotice.Size = new System.Drawing.Size(294, 13);
            this.lblNotice.TabIndex = 6;
            this.lblNotice.Text = "Notice! This is not the active \"Implant Set\" of your character.";
            //
            // ImplantCalculator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.ClientSize = new System.Drawing.Size(553, 225);
            this.Controls.Add(this.lblNotice);
            this.Controls.Add(this.gbWithThisAttributes);
            this.Controls.Add(this.gbCurrent);
            this.Controls.Add(this.gbCurrentBase);
            this.Controls.Add(this.gbAttributes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ImplantCalculator";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Implant Calculator";
            this.gbAttributes.ResumeLayout(false);
            this.AtrributesPanel.ResumeLayout(false);
            this.AtrributesPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIntelligence)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCharisma)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPerception)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMemory)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudWillpower)).EndInit();
            this.gbCurrentBase.ResumeLayout(false);
            this.gbCurrentBase.PerformLayout();
            this.gbCurrent.ResumeLayout(false);
            this.gbCurrent.PerformLayout();
            this.gbWithThisAttributes.ResumeLayout(false);
            this.gbWithThisAttributes.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbAttributes;
        private System.Windows.Forms.NumericUpDown nudIntelligence;
        private System.Windows.Forms.Label lblIntelligence;
        private System.Windows.Forms.Label lbWillpower;
        private System.Windows.Forms.Label lblMemory;
        private System.Windows.Forms.Label lblPerception;
        private System.Windows.Forms.Label lblCharisma;
        private System.Windows.Forms.NumericUpDown nudWillpower;
        private System.Windows.Forms.NumericUpDown nudMemory;
        private System.Windows.Forms.NumericUpDown nudPerception;
        private System.Windows.Forms.NumericUpDown nudCharisma;
        private System.Windows.Forms.Label lblImplants;
        private System.Windows.Forms.Label lblEffectiveWillpower;
        private System.Windows.Forms.Label lblEffectiveMemory;
        private System.Windows.Forms.Label lblEffectivePerception;
        private System.Windows.Forms.Label lblEffectiveCharisma;
        private System.Windows.Forms.Label lblEffectiveIntelligence;
        private System.Windows.Forms.Label lblEffective;
        private System.Windows.Forms.Label lblAdjustWillpower;
        private System.Windows.Forms.Label lblAdjustMemory;
        private System.Windows.Forms.Label lblAdjustPerception;
        private System.Windows.Forms.Label lblAdjustCharisma;
        private System.Windows.Forms.Label lblAdjustIntelligence;
        private System.Windows.Forms.GroupBox gbCurrentBase;
        private System.Windows.Forms.Label lblBaseDate;
        private System.Windows.Forms.Label lblBaseSpan;
        private System.Windows.Forms.GroupBox gbCurrent;
        private System.Windows.Forms.Label lblCurrentDate;
        private System.Windows.Forms.Label lblCurrentSpan;
        private System.Windows.Forms.GroupBox gbWithThisAttributes;
        private System.Windows.Forms.Label lblThisDate;
        private System.Windows.Forms.Label lblThisSpan;
        private System.Windows.Forms.Panel AtrributesPanel;
        private System.Windows.Forms.Label lblComparedToCurrent;
        private System.Windows.Forms.Label lblComparedToBase;
        private System.Windows.Forms.ToolStripMenuItem miNoImplants;
        private System.Windows.Forms.ToolStripMenuItem miCurrentClone;
        private System.Windows.Forms.ToolStripMenuItem miJumpClone1;
        private System.Windows.Forms.ToolStripMenuItem miJumpClone2;
        private System.Windows.Forms.ToolStripMenuItem miJumpClone3;
        private System.Windows.Forms.ToolStripMenuItem miJumpClone4;
        private System.Windows.Forms.ToolStripMenuItem miJumpClone5;
        private System.Windows.Forms.Label lblNotice;
    }
}
