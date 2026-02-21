// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.SettingsUI
{
    partial class ExternalCalendarControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExternalCalendarControl));
            this.externalCalendarPanel = new System.Windows.Forms.Panel();
            this.gbReminder = new System.Windows.Forms.GroupBox();
            this.lblMinutes = new System.Windows.Forms.Label();
            this.cbSetReminder = new System.Windows.Forms.CheckBox();
            this.lblLateReminder = new System.Windows.Forms.Label();
            this.dtpLateReminder = new System.Windows.Forms.DateTimePicker();
            this.lblEarlyReminder = new System.Windows.Forms.Label();
            this.tbReminder = new System.Windows.Forms.TextBox();
            this.cbUseAlterateReminder = new System.Windows.Forms.CheckBox();
            this.dtpEarlyReminder = new System.Windows.Forms.DateTimePicker();
            this.cbLastQueuedSkillOnly = new System.Windows.Forms.CheckBox();
            this.rbMSOutlook = new System.Windows.Forms.RadioButton();
            this.rbGoogle = new System.Windows.Forms.RadioButton();
            this.gbGoogle = new System.Windows.Forms.GroupBox();
            this.calendarIDLinkLabel = new System.Windows.Forms.LinkLabel();
            this.apiResponseLabel = new System.Windows.Forms.Label();
            this.btnRevokeAuth = new System.Windows.Forms.Button();
            this.btnRequestAuth = new System.Windows.Forms.Button();
            this.cbGoogleReminder = new System.Windows.Forms.ComboBox();
            this.lblReminder = new System.Windows.Forms.Label();
            this.lblCalendarId = new System.Windows.Forms.Label();
            this.tbGoogleCalendarName = new System.Windows.Forms.TextBox();
            this.gbMSOutlook = new System.Windows.Forms.GroupBox();
            this.calendarPathExampleLabel = new System.Windows.Forms.Label();
            this.rbCustomCalendar = new System.Windows.Forms.RadioButton();
            this.rbDefaultCalendar = new System.Windows.Forms.RadioButton();
            this.tbOutlookCalendarPath = new System.Windows.Forms.TextBox();
            this.calendarPathLabel = new System.Windows.Forms.Label();
            this.throbber = new EVEMon.Common.Controls.Throbber();
            this.externalCalendarPanel.SuspendLayout();
            this.gbReminder.SuspendLayout();
            this.gbGoogle.SuspendLayout();
            this.gbMSOutlook.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.throbber)).BeginInit();
            this.SuspendLayout();
            // 
            // externalCalendarPanel
            // 
            this.externalCalendarPanel.Controls.Add(this.gbReminder);
            this.externalCalendarPanel.Controls.Add(this.cbLastQueuedSkillOnly);
            this.externalCalendarPanel.Controls.Add(this.rbMSOutlook);
            this.externalCalendarPanel.Controls.Add(this.rbGoogle);
            this.externalCalendarPanel.Controls.Add(this.gbGoogle);
            this.externalCalendarPanel.Controls.Add(this.gbMSOutlook);
            this.externalCalendarPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.externalCalendarPanel.Location = new System.Drawing.Point(0, 0);
            this.externalCalendarPanel.Name = "externalCalendarPanel";
            this.externalCalendarPanel.Padding = new System.Windows.Forms.Padding(3);
            this.externalCalendarPanel.Size = new System.Drawing.Size(439, 297);
            this.externalCalendarPanel.TabIndex = 13;
            // 
            // gbReminder
            // 
            this.gbReminder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbReminder.Controls.Add(this.lblMinutes);
            this.gbReminder.Controls.Add(this.cbSetReminder);
            this.gbReminder.Controls.Add(this.lblLateReminder);
            this.gbReminder.Controls.Add(this.dtpLateReminder);
            this.gbReminder.Controls.Add(this.lblEarlyReminder);
            this.gbReminder.Controls.Add(this.tbReminder);
            this.gbReminder.Controls.Add(this.cbUseAlterateReminder);
            this.gbReminder.Controls.Add(this.dtpEarlyReminder);
            this.gbReminder.Location = new System.Drawing.Point(6, 169);
            this.gbReminder.Name = "gbReminder";
            this.gbReminder.Size = new System.Drawing.Size(426, 96);
            this.gbReminder.TabIndex = 13;
            this.gbReminder.TabStop = false;
            this.gbReminder.Text = "Reminder Setting";
            // 
            // lblMinutes
            // 
            this.lblMinutes.AutoSize = true;
            this.lblMinutes.Location = new System.Drawing.Point(144, 21);
            this.lblMinutes.Name = "lblMinutes";
            this.lblMinutes.Size = new System.Drawing.Size(43, 13);
            this.lblMinutes.TabIndex = 11;
            this.lblMinutes.Text = "minutes";
            // 
            // cbSetReminder
            // 
            this.cbSetReminder.AutoSize = true;
            this.cbSetReminder.Location = new System.Drawing.Point(8, 20);
            this.cbSetReminder.Name = "cbSetReminder";
            this.cbSetReminder.Size = new System.Drawing.Size(88, 17);
            this.cbSetReminder.TabIndex = 6;
            this.cbSetReminder.Text = "Use reminder";
            this.cbSetReminder.UseVisualStyleBackColor = true;
            this.cbSetReminder.Click += new System.EventHandler(this.OnMustEnableOrDisable);
            // 
            // lblLateReminder
            // 
            this.lblLateReminder.AutoSize = true;
            this.lblLateReminder.Location = new System.Drawing.Point(192, 70);
            this.lblLateReminder.Name = "lblLateReminder";
            this.lblLateReminder.Size = new System.Drawing.Size(79, 13);
            this.lblLateReminder.TabIndex = 6;
            this.lblLateReminder.Text = "Late Reminder:";
            // 
            // dtpLateReminder
            // 
            this.dtpLateReminder.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpLateReminder.Location = new System.Drawing.Point(278, 66);
            this.dtpLateReminder.Name = "dtpLateReminder";
            this.dtpLateReminder.ShowUpDown = true;
            this.dtpLateReminder.Size = new System.Drawing.Size(72, 20);
            this.dtpLateReminder.TabIndex = 10;
            this.dtpLateReminder.Value = new System.DateTime(2007, 9, 21, 0, 0, 0, 0);
            // 
            // lblEarlyReminder
            // 
            this.lblEarlyReminder.AutoSize = true;
            this.lblEarlyReminder.Location = new System.Drawing.Point(27, 70);
            this.lblEarlyReminder.Name = "lblEarlyReminder";
            this.lblEarlyReminder.Size = new System.Drawing.Size(81, 13);
            this.lblEarlyReminder.TabIndex = 4;
            this.lblEarlyReminder.Text = "Early Reminder:";
            // 
            // tbReminder
            // 
            this.tbReminder.Location = new System.Drawing.Point(102, 18);
            this.tbReminder.Name = "tbReminder";
            this.tbReminder.Size = new System.Drawing.Size(35, 20);
            this.tbReminder.TabIndex = 7;
            this.tbReminder.Text = "10";
            this.tbReminder.Validating += new System.ComponentModel.CancelEventHandler(this.tbReminder_Validating);
            // 
            // cbUseAlterateReminder
            // 
            this.cbUseAlterateReminder.AutoSize = true;
            this.cbUseAlterateReminder.Location = new System.Drawing.Point(8, 45);
            this.cbUseAlterateReminder.Name = "cbUseAlterateReminder";
            this.cbUseAlterateReminder.Size = new System.Drawing.Size(132, 17);
            this.cbUseAlterateReminder.TabIndex = 8;
            this.cbUseAlterateReminder.Text = "Use alternate reminder";
            this.cbUseAlterateReminder.UseVisualStyleBackColor = true;
            this.cbUseAlterateReminder.Click += new System.EventHandler(this.OnMustEnableOrDisable);
            // 
            // dtpEarlyReminder
            // 
            this.dtpEarlyReminder.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpEarlyReminder.Location = new System.Drawing.Point(116, 66);
            this.dtpEarlyReminder.Name = "dtpEarlyReminder";
            this.dtpEarlyReminder.ShowUpDown = true;
            this.dtpEarlyReminder.Size = new System.Drawing.Size(70, 20);
            this.dtpEarlyReminder.TabIndex = 9;
            this.dtpEarlyReminder.Value = new System.DateTime(2007, 9, 21, 0, 0, 0, 0);
            // 
            // cbLastQueuedSkillOnly
            // 
            this.cbLastQueuedSkillOnly.AutoSize = true;
            this.cbLastQueuedSkillOnly.Location = new System.Drawing.Point(14, 271);
            this.cbLastQueuedSkillOnly.Name = "cbLastQueuedSkillOnly";
            this.cbLastQueuedSkillOnly.Size = new System.Drawing.Size(133, 17);
            this.cbLastQueuedSkillOnly.TabIndex = 12;
            this.cbLastQueuedSkillOnly.Text = "Last Queued Skill Only";
            this.cbLastQueuedSkillOnly.UseVisualStyleBackColor = true;
            // 
            // rbMSOutlook
            // 
            this.rbMSOutlook.AutoSize = true;
            this.rbMSOutlook.CausesValidation = false;
            this.rbMSOutlook.Location = new System.Drawing.Point(6, 8);
            this.rbMSOutlook.Name = "rbMSOutlook";
            this.rbMSOutlook.Size = new System.Drawing.Size(81, 17);
            this.rbMSOutlook.TabIndex = 1;
            this.rbMSOutlook.Text = "MS Outlook";
            this.rbMSOutlook.UseVisualStyleBackColor = true;
            this.rbMSOutlook.CheckedChanged += new System.EventHandler(this.OnMustEnableOrDisable);
            // 
            // rbGoogle
            // 
            this.rbGoogle.AutoSize = true;
            this.rbGoogle.CausesValidation = false;
            this.rbGoogle.Location = new System.Drawing.Point(93, 8);
            this.rbGoogle.Name = "rbGoogle";
            this.rbGoogle.Size = new System.Drawing.Size(59, 17);
            this.rbGoogle.TabIndex = 2;
            this.rbGoogle.Text = "Google";
            this.rbGoogle.UseVisualStyleBackColor = true;
            this.rbGoogle.CheckedChanged += new System.EventHandler(this.rbGoogle_CheckedChanged);
            // 
            // gbGoogle
            // 
            this.gbGoogle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbGoogle.Controls.Add(this.calendarIDLinkLabel);
            this.gbGoogle.Controls.Add(this.apiResponseLabel);
            this.gbGoogle.Controls.Add(this.btnRevokeAuth);
            this.gbGoogle.Controls.Add(this.btnRequestAuth);
            this.gbGoogle.Controls.Add(this.cbGoogleReminder);
            this.gbGoogle.Controls.Add(this.lblReminder);
            this.gbGoogle.Controls.Add(this.lblCalendarId);
            this.gbGoogle.Controls.Add(this.tbGoogleCalendarName);
            this.gbGoogle.Controls.Add(this.throbber);
            this.gbGoogle.Location = new System.Drawing.Point(5, 31);
            this.gbGoogle.Name = "gbGoogle";
            this.gbGoogle.Size = new System.Drawing.Size(427, 137);
            this.gbGoogle.TabIndex = 3;
            this.gbGoogle.TabStop = false;
            this.gbGoogle.Text = "Google Information";
            // 
            // calendarIDLinkLabel
            // 
            this.calendarIDLinkLabel.AutoSize = true;
            this.calendarIDLinkLabel.LinkArea = new System.Windows.Forms.LinkArea(54, 25);
            this.calendarIDLinkLabel.Location = new System.Drawing.Point(7, 118);
            this.calendarIDLinkLabel.Name = "calendarIDLinkLabel";
            this.calendarIDLinkLabel.Size = new System.Drawing.Size(406, 17);
            this.calendarIDLinkLabel.TabIndex = 12;
            this.calendarIDLinkLabel.TabStop = true;
            this.calendarIDLinkLabel.Text = "Tip: Leave Calendar ID blank to use default calendar. How to find a Calendar ID.";
            this.calendarIDLinkLabel.UseCompatibleTextRendering = true;
            this.calendarIDLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.calendarIDLinkLabel_LinkClicked);
            // 
            // apiResponseLabel
            // 
            this.apiResponseLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.apiResponseLabel.Location = new System.Drawing.Point(6, 16);
            this.apiResponseLabel.Name = "apiResponseLabel";
            this.apiResponseLabel.Size = new System.Drawing.Size(415, 24);
            this.apiResponseLabel.TabIndex = 11;
            this.apiResponseLabel.Text = "APIResponse";
            this.apiResponseLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnRevokeAuth
            // 
            this.btnRevokeAuth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRevokeAuth.Location = new System.Drawing.Point(347, 43);
            this.btnRevokeAuth.Name = "btnRevokeAuth";
            this.btnRevokeAuth.Size = new System.Drawing.Size(75, 23);
            this.btnRevokeAuth.TabIndex = 9;
            this.btnRevokeAuth.Text = "Reset";
            this.btnRevokeAuth.UseVisualStyleBackColor = true;
            this.btnRevokeAuth.Click += new System.EventHandler(this.btnRevokeAuth_Click);
            // 
            // btnRequestAuth
            // 
            this.btnRequestAuth.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnRequestAuth.Location = new System.Drawing.Point(140, 43);
            this.btnRequestAuth.Name = "btnRequestAuth";
            this.btnRequestAuth.Size = new System.Drawing.Size(147, 23);
            this.btnRequestAuth.TabIndex = 8;
            this.btnRequestAuth.Text = "Request Authentication";
            this.btnRequestAuth.UseVisualStyleBackColor = true;
            this.btnRequestAuth.Click += new System.EventHandler(this.btnRequestAuth_Click);
            // 
            // cbGoogleReminder
            // 
            this.cbGoogleReminder.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbGoogleReminder.FormattingEnabled = true;
            this.cbGoogleReminder.Location = new System.Drawing.Point(78, 93);
            this.cbGoogleReminder.Name = "cbGoogleReminder";
            this.cbGoogleReminder.Size = new System.Drawing.Size(92, 21);
            this.cbGoogleReminder.TabIndex = 7;
            // 
            // lblReminder
            // 
            this.lblReminder.AutoSize = true;
            this.lblReminder.Location = new System.Drawing.Point(6, 96);
            this.lblReminder.Name = "lblReminder";
            this.lblReminder.Size = new System.Drawing.Size(55, 13);
            this.lblReminder.TabIndex = 6;
            this.lblReminder.Text = "Reminder:";
            // 
            // lblCalendarId
            // 
            this.lblCalendarId.AutoSize = true;
            this.lblCalendarId.Location = new System.Drawing.Point(6, 73);
            this.lblCalendarId.Name = "lblCalendarId";
            this.lblCalendarId.Size = new System.Drawing.Size(66, 13);
            this.lblCalendarId.TabIndex = 5;
            this.lblCalendarId.Text = "Calendar ID:";
            // 
            // tbGoogleCalendarName
            // 
            this.tbGoogleCalendarName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbGoogleCalendarName.Location = new System.Drawing.Point(78, 69);
            this.tbGoogleCalendarName.Name = "tbGoogleCalendarName";
            this.tbGoogleCalendarName.Size = new System.Drawing.Size(343, 20);
            this.tbGoogleCalendarName.TabIndex = 5;
            this.tbGoogleCalendarName.Text = "primary";
            // 
            // gbMSOutlook
            // 
            this.gbMSOutlook.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbMSOutlook.Controls.Add(this.calendarPathExampleLabel);
            this.gbMSOutlook.Controls.Add(this.rbCustomCalendar);
            this.gbMSOutlook.Controls.Add(this.rbDefaultCalendar);
            this.gbMSOutlook.Controls.Add(this.tbOutlookCalendarPath);
            this.gbMSOutlook.Controls.Add(this.calendarPathLabel);
            this.gbMSOutlook.Location = new System.Drawing.Point(5, 31);
            this.gbMSOutlook.Name = "gbMSOutlook";
            this.gbMSOutlook.Size = new System.Drawing.Size(427, 137);
            this.gbMSOutlook.TabIndex = 14;
            this.gbMSOutlook.TabStop = false;
            this.gbMSOutlook.Text = "MS Outlook Information";
            // 
            // calendarPathExampleLabel
            // 
            this.calendarPathExampleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.calendarPathExampleLabel.AutoSize = true;
            this.calendarPathExampleLabel.Location = new System.Drawing.Point(6, 74);
            this.calendarPathExampleLabel.Name = "calendarPathExampleLabel";
            this.calendarPathExampleLabel.Size = new System.Drawing.Size(411, 52);
            this.calendarPathExampleLabel.TabIndex = 4;
            this.calendarPathExampleLabel.Text = resources.GetString("calendarPathExampleLabel.Text");
            // 
            // rbCustomCalendar
            // 
            this.rbCustomCalendar.AutoSize = true;
            this.rbCustomCalendar.Location = new System.Drawing.Point(141, 20);
            this.rbCustomCalendar.Name = "rbCustomCalendar";
            this.rbCustomCalendar.Size = new System.Drawing.Size(127, 17);
            this.rbCustomCalendar.TabIndex = 3;
            this.rbCustomCalendar.TabStop = true;
            this.rbCustomCalendar.Text = "Use Custom Calendar";
            this.rbCustomCalendar.UseVisualStyleBackColor = true;
            this.rbCustomCalendar.Click += new System.EventHandler(this.OnMustEnableOrDisable);
            // 
            // rbDefaultCalendar
            // 
            this.rbDefaultCalendar.AutoSize = true;
            this.rbDefaultCalendar.CausesValidation = false;
            this.rbDefaultCalendar.Location = new System.Drawing.Point(9, 20);
            this.rbDefaultCalendar.Name = "rbDefaultCalendar";
            this.rbDefaultCalendar.Size = new System.Drawing.Size(126, 17);
            this.rbDefaultCalendar.TabIndex = 2;
            this.rbDefaultCalendar.TabStop = true;
            this.rbDefaultCalendar.Text = "Use Default Calendar";
            this.rbDefaultCalendar.UseVisualStyleBackColor = true;
            this.rbDefaultCalendar.Click += new System.EventHandler(this.OnMustEnableOrDisable);
            // 
            // tbOutlookCalendarPath
            // 
            this.tbOutlookCalendarPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbOutlookCalendarPath.Location = new System.Drawing.Point(95, 46);
            this.tbOutlookCalendarPath.Name = "tbOutlookCalendarPath";
            this.tbOutlookCalendarPath.Size = new System.Drawing.Size(326, 20);
            this.tbOutlookCalendarPath.TabIndex = 1;
            this.tbOutlookCalendarPath.Validating += new System.ComponentModel.CancelEventHandler(this.tbCalendarPath_Validating);
            // 
            // calendarPathLabel
            // 
            this.calendarPathLabel.AutoSize = true;
            this.calendarPathLabel.Location = new System.Drawing.Point(6, 49);
            this.calendarPathLabel.Name = "calendarPathLabel";
            this.calendarPathLabel.Size = new System.Drawing.Size(77, 13);
            this.calendarPathLabel.TabIndex = 0;
            this.calendarPathLabel.Text = "Calendar Path:";
            // 
            // throbber
            // 
            this.throbber.Location = new System.Drawing.Point(201, 16);
            this.throbber.MaximumSize = new System.Drawing.Size(24, 24);
            this.throbber.MinimumSize = new System.Drawing.Size(24, 24);
            this.throbber.Name = "throbber";
            this.throbber.Size = new System.Drawing.Size(24, 24);
            this.throbber.State = EVEMon.Common.Enumerations.ThrobberState.Stopped;
            this.throbber.TabIndex = 13;
            this.throbber.TabStop = false;
            // 
            // ExternalCalendarControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.externalCalendarPanel);
            this.Enabled = false;
            this.Name = "ExternalCalendarControl";
            this.Size = new System.Drawing.Size(439, 297);
            this.EnabledChanged += new System.EventHandler(this.ExternalCalendarControl_EnabledChanged);
            this.externalCalendarPanel.ResumeLayout(false);
            this.externalCalendarPanel.PerformLayout();
            this.gbReminder.ResumeLayout(false);
            this.gbReminder.PerformLayout();
            this.gbGoogle.ResumeLayout(false);
            this.gbGoogle.PerformLayout();
            this.gbMSOutlook.ResumeLayout(false);
            this.gbMSOutlook.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.throbber)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox gbReminder;
        private System.Windows.Forms.Label lblMinutes;
        private System.Windows.Forms.CheckBox cbSetReminder;
        private System.Windows.Forms.Label lblLateReminder;
        private System.Windows.Forms.DateTimePicker dtpLateReminder;
        private System.Windows.Forms.Label lblEarlyReminder;
        private System.Windows.Forms.TextBox tbReminder;
        private System.Windows.Forms.CheckBox cbUseAlterateReminder;
        private System.Windows.Forms.DateTimePicker dtpEarlyReminder;
        private System.Windows.Forms.CheckBox cbLastQueuedSkillOnly;
        private System.Windows.Forms.RadioButton rbMSOutlook;
        private System.Windows.Forms.RadioButton rbGoogle;
        private System.Windows.Forms.GroupBox gbGoogle;
        private System.Windows.Forms.ComboBox cbGoogleReminder;
        private System.Windows.Forms.Label lblReminder;
        private System.Windows.Forms.Label lblCalendarId;
        private System.Windows.Forms.TextBox tbGoogleCalendarName;
        private System.Windows.Forms.GroupBox gbMSOutlook;
        private System.Windows.Forms.Label calendarPathExampleLabel;
        private System.Windows.Forms.RadioButton rbCustomCalendar;
        private System.Windows.Forms.RadioButton rbDefaultCalendar;
        private System.Windows.Forms.TextBox tbOutlookCalendarPath;
        private System.Windows.Forms.Label calendarPathLabel;
        private System.Windows.Forms.Panel externalCalendarPanel;
        private System.Windows.Forms.Button btnRevokeAuth;
        private System.Windows.Forms.Button btnRequestAuth;
        private System.Windows.Forms.Label apiResponseLabel;
        private System.Windows.Forms.LinkLabel calendarIDLinkLabel;
        private Common.Controls.Throbber throbber;
    }
}
