// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Windows.Forms;

namespace EVEMon.Common.Controls
{
    partial class PrintOptionsDialog
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
            this.comboPrinters = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkEntryNumber = new System.Windows.Forms.CheckBox();
            this.checkStartDate = new System.Windows.Forms.CheckBox();
            this.checkTrainingTimes = new System.Windows.Forms.CheckBox();
            this.checkFinishDate = new System.Windows.Forms.CheckBox();
            this.checkPageHeaders = new System.Windows.Forms.CheckBox();
            this.checkPageNumbers = new System.Windows.Forms.CheckBox();
            this.checkDateInformation = new System.Windows.Forms.CheckBox();
            this.checkTotalTimes = new System.Windows.Forms.CheckBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.btn_setDefs = new System.Windows.Forms.Button();
            this.checkNotes = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // comboPrinters
            // 
            this.comboPrinters.FormattingEnabled = true;
            this.comboPrinters.Location = new System.Drawing.Point(55, 17);
            this.comboPrinters.Name = "comboPrinters";
            this.comboPrinters.Size = new System.Drawing.Size(215, 21);
            this.comboPrinters.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Printer";
            // 
            // checkEntryNumber
            // 
            this.checkEntryNumber.AutoSize = true;
            this.checkEntryNumber.Location = new System.Drawing.Point(55, 44);
            this.checkEntryNumber.Name = "checkEntryNumber";
            this.checkEntryNumber.Size = new System.Drawing.Size(114, 17);
            this.checkEntryNumber.TabIndex = 2;
            this.checkEntryNumber.Text = "Print Entry Number";
            this.checkEntryNumber.UseVisualStyleBackColor = true;
            // 
            // checkStartDate
            // 
            this.checkStartDate.AutoSize = true;
            this.checkStartDate.Location = new System.Drawing.Point(55, 90);
            this.checkStartDate.Name = "checkStartDate";
            this.checkStartDate.Size = new System.Drawing.Size(98, 17);
            this.checkStartDate.TabIndex = 3;
            this.checkStartDate.Text = "Print Start Date";
            this.checkStartDate.UseVisualStyleBackColor = true;
            // 
            // checkTrainingTimes
            // 
            this.checkTrainingTimes.AutoSize = true;
            this.checkTrainingTimes.Location = new System.Drawing.Point(55, 113);
            this.checkTrainingTimes.Name = "checkTrainingTimes";
            this.checkTrainingTimes.Size = new System.Drawing.Size(119, 17);
            this.checkTrainingTimes.TabIndex = 4;
            this.checkTrainingTimes.Text = "Print Training Times";
            this.checkTrainingTimes.UseVisualStyleBackColor = true;
            // 
            // checkFinishDate
            // 
            this.checkFinishDate.AutoSize = true;
            this.checkFinishDate.Location = new System.Drawing.Point(55, 136);
            this.checkFinishDate.Name = "checkFinishDate";
            this.checkFinishDate.Size = new System.Drawing.Size(103, 17);
            this.checkFinishDate.TabIndex = 5;
            this.checkFinishDate.Text = "Print Finish Date";
            this.checkFinishDate.UseVisualStyleBackColor = true;
            // 
            // checkPageHeaders
            // 
            this.checkPageHeaders.AutoSize = true;
            this.checkPageHeaders.Location = new System.Drawing.Point(55, 159);
            this.checkPageHeaders.Name = "checkPageHeaders";
            this.checkPageHeaders.Size = new System.Drawing.Size(118, 17);
            this.checkPageHeaders.TabIndex = 6;
            this.checkPageHeaders.Text = "Print Page Headers";
            this.checkPageHeaders.UseVisualStyleBackColor = true;
            // 
            // checkPageNumbers
            // 
            this.checkPageNumbers.AutoSize = true;
            this.checkPageNumbers.Location = new System.Drawing.Point(55, 182);
            this.checkPageNumbers.Name = "checkPageNumbers";
            this.checkPageNumbers.Size = new System.Drawing.Size(120, 17);
            this.checkPageNumbers.TabIndex = 7;
            this.checkPageNumbers.Text = "Print Page Numbers";
            this.checkPageNumbers.UseVisualStyleBackColor = true;
            // 
            // checkDateInformation
            // 
            this.checkDateInformation.AutoSize = true;
            this.checkDateInformation.Location = new System.Drawing.Point(55, 205);
            this.checkDateInformation.Name = "checkDateInformation";
            this.checkDateInformation.Size = new System.Drawing.Size(128, 17);
            this.checkDateInformation.TabIndex = 8;
            this.checkDateInformation.Text = "Print Date Information";
            this.checkDateInformation.UseVisualStyleBackColor = true;
            // 
            // checkTotalTimes
            // 
            this.checkTotalTimes.AutoSize = true;
            this.checkTotalTimes.Location = new System.Drawing.Point(55, 228);
            this.checkTotalTimes.Name = "checkTotalTimes";
            this.checkTotalTimes.Size = new System.Drawing.Size(146, 17);
            this.checkTotalTimes.TabIndex = 9;
            this.checkTotalTimes.Text = "Print Total Training Times";
            this.checkTotalTimes.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(39, 300);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 10;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.OnAccept);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(171, 300);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 11;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // btn_setDefs
            // 
            this.btn_setDefs.Location = new System.Drawing.Point(39, 261);
            this.btn_setDefs.Name = "btn_setDefs";
            this.btn_setDefs.Size = new System.Drawing.Size(207, 23);
            this.btn_setDefs.TabIndex = 12;
            this.btn_setDefs.Text = "Set as Default Options";
            this.btn_setDefs.UseVisualStyleBackColor = true;
            this.btn_setDefs.Click += new System.EventHandler(this.OnSetAsDefaults);
            // 
            // checkNotes
            // 
            this.checkNotes.AutoSize = true;
            this.checkNotes.Location = new System.Drawing.Point(55, 67);
            this.checkNotes.Name = "checkNotes";
            this.checkNotes.Size = new System.Drawing.Size(78, 17);
            this.checkNotes.TabIndex = 13;
            this.checkNotes.Text = "Print Notes";
            this.checkNotes.UseVisualStyleBackColor = true;
            // 
            // PrintOptionsDialog
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ((Form)this).CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(290, 339);
            this.Controls.Add(this.checkNotes);
            this.Controls.Add(this.btn_setDefs);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.checkTotalTimes);
            this.Controls.Add(this.checkDateInformation);
            this.Controls.Add(this.checkPageNumbers);
            this.Controls.Add(this.checkPageHeaders);
            this.Controls.Add(this.checkFinishDate);
            this.Controls.Add(this.checkTrainingTimes);
            this.Controls.Add(this.checkStartDate);
            this.Controls.Add(this.checkEntryNumber);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboPrinters);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrintOptionsDialog";
            this.Text = "Skill Printer Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboPrinters;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkEntryNumber;
        private System.Windows.Forms.CheckBox checkStartDate;
        private System.Windows.Forms.CheckBox checkTrainingTimes;
        private System.Windows.Forms.CheckBox checkFinishDate;
        private System.Windows.Forms.CheckBox checkPageHeaders;
        private System.Windows.Forms.CheckBox checkPageNumbers;
        private System.Windows.Forms.CheckBox checkDateInformation;
        private System.Windows.Forms.CheckBox checkTotalTimes;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button btn_setDefs;
        private System.Windows.Forms.CheckBox checkNotes;
    }
}
