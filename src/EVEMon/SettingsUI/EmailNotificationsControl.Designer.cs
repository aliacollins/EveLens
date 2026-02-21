// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.SettingsUI
{
    partial class EmailNotificationsControl
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
            this.components = new System.ComponentModel.Container();
            this.cbEmailUseShortFormat = new System.Windows.Forms.CheckBox();
            this.tlpEmailSettings = new System.Windows.Forms.TableLayoutPanel();
            this.lblEmailProvider = new System.Windows.Forms.Label();
            this.lblFromAddress = new System.Windows.Forms.Label();
            this.lblToAddress = new System.Windows.Forms.Label();
            this.cbbEMailServerProvider = new System.Windows.Forms.ComboBox();
            this.tbFromAddress = new System.Windows.Forms.TextBox();
            this.tbToAddress = new System.Windows.Forms.TextBox();
            this.tlpEmailAuthTable = new System.Windows.Forms.TableLayoutPanel();
            this.lblEmailPassword = new System.Windows.Forms.Label();
            this.lblEmailUsername = new System.Windows.Forms.Label();
            this.tbEmailUsername = new System.Windows.Forms.TextBox();
            this.tbEmailPassword = new System.Windows.Forms.TextBox();
            this.tlpEmailServerSettings = new System.Windows.Forms.TableLayoutPanel();
            this.lblEmailServerAddress = new System.Windows.Forms.Label();
            this.tbEmailServerAddress = new System.Windows.Forms.TextBox();
            this.lblPortNumber = new System.Windows.Forms.Label();
            this.tbEmailPort = new System.Windows.Forms.TextBox();
            this.cbEmailServerRequireSsl = new System.Windows.Forms.CheckBox();
            this.cbEmailAuthRequired = new System.Windows.Forms.CheckBox();
            this.lblToAddressInfo = new System.Windows.Forms.Label();
            this.btnTestEmail = new System.Windows.Forms.Button();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.btnReset = new System.Windows.Forms.Button();
            this.tlpEmailSettings.SuspendLayout();
            this.tlpEmailAuthTable.SuspendLayout();
            this.tlpEmailServerSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // cbEmailUseShortFormat
            // 
            this.cbEmailUseShortFormat.AutoSize = true;
            this.cbEmailUseShortFormat.Location = new System.Drawing.Point(12, 3);
            this.cbEmailUseShortFormat.Name = "cbEmailUseShortFormat";
            this.cbEmailUseShortFormat.Size = new System.Drawing.Size(179, 17);
            this.cbEmailUseShortFormat.TabIndex = 0;
            this.cbEmailUseShortFormat.Text = "Use Short Format (SMS-Friendly)";
            this.cbEmailUseShortFormat.UseVisualStyleBackColor = true;
            // 
            // tlpEmailSettings
            // 
            this.tlpEmailSettings.AutoSize = true;
            this.tlpEmailSettings.ColumnCount = 2;
            this.tlpEmailSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 88F));
            this.tlpEmailSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpEmailSettings.Controls.Add(this.lblEmailProvider, 0, 0);
            this.tlpEmailSettings.Controls.Add(this.lblFromAddress, 0, 3);
            this.tlpEmailSettings.Controls.Add(this.lblToAddress, 0, 4);
            this.tlpEmailSettings.Controls.Add(this.cbbEMailServerProvider, 1, 0);
            this.tlpEmailSettings.Controls.Add(this.tbFromAddress, 1, 3);
            this.tlpEmailSettings.Controls.Add(this.tbToAddress, 1, 4);
            this.tlpEmailSettings.Controls.Add(this.tlpEmailAuthTable, 1, 2);
            this.tlpEmailSettings.Controls.Add(this.tlpEmailServerSettings, 0, 1);
            this.tlpEmailSettings.Controls.Add(this.lblToAddressInfo, 0, 5);
            this.tlpEmailSettings.Location = new System.Drawing.Point(26, 26);
            this.tlpEmailSettings.Name = "tlpEmailSettings";
            this.tlpEmailSettings.RowCount = 6;
            this.tlpEmailSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailSettings.Size = new System.Drawing.Size(304, 274);
            this.tlpEmailSettings.TabIndex = 1;
            // 
            // lblEmailProvider
            // 
            this.lblEmailProvider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEmailProvider.AutoSize = true;
            this.lblEmailProvider.Location = new System.Drawing.Point(8, 0);
            this.lblEmailProvider.Name = "lblEmailProvider";
            this.lblEmailProvider.Size = new System.Drawing.Size(77, 27);
            this.lblEmailProvider.TabIndex = 0;
            this.lblEmailProvider.Text = "Email Provider:";
            this.lblEmailProvider.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblFromAddress
            // 
            this.lblFromAddress.AutoSize = true;
            this.lblFromAddress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFromAddress.Location = new System.Drawing.Point(3, 182);
            this.lblFromAddress.Name = "lblFromAddress";
            this.lblFromAddress.Size = new System.Drawing.Size(82, 26);
            this.lblFromAddress.TabIndex = 1;
            this.lblFromAddress.Text = "From address:";
            this.lblFromAddress.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblToAddress
            // 
            this.lblToAddress.AutoSize = true;
            this.lblToAddress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblToAddress.Location = new System.Drawing.Point(3, 208);
            this.lblToAddress.Name = "lblToAddress";
            this.lblToAddress.Size = new System.Drawing.Size(82, 53);
            this.lblToAddress.TabIndex = 2;
            this.lblToAddress.Text = "To address:";
            this.lblToAddress.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cbbEMailServerProvider
            // 
            this.cbbEMailServerProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbEMailServerProvider.Location = new System.Drawing.Point(91, 3);
            this.cbbEMailServerProvider.Name = "cbbEMailServerProvider";
            this.cbbEMailServerProvider.Size = new System.Drawing.Size(122, 21);
            this.cbbEMailServerProvider.TabIndex = 0;
            this.cbbEMailServerProvider.SelectedIndexChanged += new System.EventHandler(this.cbbEMailServerProvider_SelectedIndexChanged);
            // 
            // tbFromAddress
            // 
            this.tbFromAddress.Location = new System.Drawing.Point(91, 185);
            this.tbFromAddress.Name = "tbFromAddress";
            this.tbFromAddress.Size = new System.Drawing.Size(181, 20);
            this.tbFromAddress.TabIndex = 3;
            this.tbFromAddress.Validating += new System.ComponentModel.CancelEventHandler(this.tbFromAddress_Validating);
            this.tbFromAddress.Validated += new System.EventHandler(this.tbFromAddress_Validated);
            // 
            // tbToAddress
            // 
            this.tbToAddress.Location = new System.Drawing.Point(91, 211);
            this.tbToAddress.Multiline = true;
            this.tbToAddress.Name = "tbToAddress";
            this.tbToAddress.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbToAddress.Size = new System.Drawing.Size(181, 47);
            this.tbToAddress.TabIndex = 4;
            this.tbToAddress.Validating += new System.ComponentModel.CancelEventHandler(this.tbToAddress_Validating);
            this.tbToAddress.Validated += new System.EventHandler(this.tbToAddress_Validated);
            // 
            // tlpEmailAuthTable
            // 
            this.tlpEmailAuthTable.AutoSize = true;
            this.tlpEmailAuthTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlpEmailAuthTable.ColumnCount = 2;
            this.tlpEmailAuthTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpEmailAuthTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpEmailAuthTable.Controls.Add(this.lblEmailPassword, 0, 1);
            this.tlpEmailAuthTable.Controls.Add(this.lblEmailUsername, 0, 0);
            this.tlpEmailAuthTable.Controls.Add(this.tbEmailUsername, 1, 0);
            this.tlpEmailAuthTable.Controls.Add(this.tbEmailPassword, 1, 1);
            this.tlpEmailAuthTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpEmailAuthTable.Location = new System.Drawing.Point(88, 125);
            this.tlpEmailAuthTable.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.tlpEmailAuthTable.Name = "tlpEmailAuthTable";
            this.tlpEmailAuthTable.RowCount = 2;
            this.tlpEmailAuthTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailAuthTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailAuthTable.Size = new System.Drawing.Size(216, 52);
            this.tlpEmailAuthTable.TabIndex = 2;
            // 
            // lblEmailPassword
            // 
            this.lblEmailPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEmailPassword.AutoSize = true;
            this.lblEmailPassword.Location = new System.Drawing.Point(5, 26);
            this.lblEmailPassword.Name = "lblEmailPassword";
            this.lblEmailPassword.Size = new System.Drawing.Size(56, 26);
            this.lblEmailPassword.TabIndex = 8;
            this.lblEmailPassword.Text = "Password:";
            this.lblEmailPassword.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblEmailUsername
            // 
            this.lblEmailUsername.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEmailUsername.AutoSize = true;
            this.lblEmailUsername.Location = new System.Drawing.Point(3, 0);
            this.lblEmailUsername.Name = "lblEmailUsername";
            this.lblEmailUsername.Size = new System.Drawing.Size(58, 26);
            this.lblEmailUsername.TabIndex = 7;
            this.lblEmailUsername.Text = "Username:";
            this.lblEmailUsername.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbEmailUsername
            // 
            this.tbEmailUsername.Location = new System.Drawing.Point(67, 3);
            this.tbEmailUsername.Name = "tbEmailUsername";
            this.tbEmailUsername.Size = new System.Drawing.Size(114, 20);
            this.tbEmailUsername.TabIndex = 0;
            this.tbEmailUsername.Validating += new System.ComponentModel.CancelEventHandler(this.tbEmailUsername_Validating);
            this.tbEmailUsername.Validated += new System.EventHandler(this.tbEmailUsername_Validated);
            // 
            // tbEmailPassword
            // 
            this.tbEmailPassword.Location = new System.Drawing.Point(67, 29);
            this.tbEmailPassword.Name = "tbEmailPassword";
            this.tbEmailPassword.PasswordChar = '*';
            this.tbEmailPassword.Size = new System.Drawing.Size(114, 20);
            this.tbEmailPassword.TabIndex = 1;
            this.tbEmailPassword.Validating += new System.ComponentModel.CancelEventHandler(this.tbEmailPassword_Validating);
            this.tbEmailPassword.Validated += new System.EventHandler(this.tbEmailPassword_Validated);
            // 
            // tlpEmailServerSettings
            // 
            this.tlpEmailServerSettings.AutoSize = true;
            this.tlpEmailServerSettings.ColumnCount = 2;
            this.tlpEmailSettings.SetColumnSpan(this.tlpEmailServerSettings, 2);
            this.tlpEmailServerSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpEmailServerSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpEmailServerSettings.Controls.Add(this.lblEmailServerAddress, 0, 0);
            this.tlpEmailServerSettings.Controls.Add(this.tbEmailServerAddress, 1, 0);
            this.tlpEmailServerSettings.Controls.Add(this.lblPortNumber, 0, 1);
            this.tlpEmailServerSettings.Controls.Add(this.tbEmailPort, 1, 1);
            this.tlpEmailServerSettings.Controls.Add(this.cbEmailServerRequireSsl, 1, 2);
            this.tlpEmailServerSettings.Controls.Add(this.cbEmailAuthRequired, 1, 3);
            this.tlpEmailServerSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpEmailServerSettings.Location = new System.Drawing.Point(0, 27);
            this.tlpEmailServerSettings.Margin = new System.Windows.Forms.Padding(0);
            this.tlpEmailServerSettings.Name = "tlpEmailServerSettings";
            this.tlpEmailServerSettings.RowCount = 4;
            this.tlpEmailServerSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailServerSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailServerSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailServerSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpEmailServerSettings.Size = new System.Drawing.Size(304, 98);
            this.tlpEmailServerSettings.TabIndex = 1;
            // 
            // lblEmailServerAddress
            // 
            this.lblEmailServerAddress.AutoSize = true;
            this.lblEmailServerAddress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmailServerAddress.Location = new System.Drawing.Point(3, 0);
            this.lblEmailServerAddress.Name = "lblEmailServerAddress";
            this.lblEmailServerAddress.Size = new System.Drawing.Size(82, 26);
            this.lblEmailServerAddress.TabIndex = 11;
            this.lblEmailServerAddress.Text = "Server Address:";
            this.lblEmailServerAddress.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbEmailServerAddress
            // 
            this.tbEmailServerAddress.Location = new System.Drawing.Point(91, 3);
            this.tbEmailServerAddress.Name = "tbEmailServerAddress";
            this.tbEmailServerAddress.Size = new System.Drawing.Size(122, 20);
            this.tbEmailServerAddress.TabIndex = 0;
            this.tbEmailServerAddress.Validating += new System.ComponentModel.CancelEventHandler(this.tbEmailServerAddress_Validating);
            this.tbEmailServerAddress.Validated += new System.EventHandler(this.tbEmailServerAddress_Validated);
            // 
            // lblPortNumber
            // 
            this.lblPortNumber.AutoSize = true;
            this.lblPortNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPortNumber.Location = new System.Drawing.Point(3, 26);
            this.lblPortNumber.Name = "lblPortNumber";
            this.lblPortNumber.Size = new System.Drawing.Size(82, 26);
            this.lblPortNumber.TabIndex = 10;
            this.lblPortNumber.Text = "Port Number:";
            this.lblPortNumber.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tbEmailPort
            // 
            this.tbEmailPort.Location = new System.Drawing.Point(91, 29);
            this.tbEmailPort.MaxLength = 5;
            this.tbEmailPort.Name = "tbEmailPort";
            this.tbEmailPort.Size = new System.Drawing.Size(40, 20);
            this.tbEmailPort.TabIndex = 1;
            this.tbEmailPort.Text = "25";
            this.tbEmailPort.Validating += new System.ComponentModel.CancelEventHandler(this.tbEmailPort_Validating);
            // 
            // cbEmailServerRequireSsl
            // 
            this.cbEmailServerRequireSsl.AutoSize = true;
            this.cbEmailServerRequireSsl.Location = new System.Drawing.Point(91, 55);
            this.cbEmailServerRequireSsl.Name = "cbEmailServerRequireSsl";
            this.cbEmailServerRequireSsl.Size = new System.Drawing.Size(117, 17);
            this.cbEmailServerRequireSsl.TabIndex = 2;
            this.cbEmailServerRequireSsl.Text = "Connect using SSL";
            this.cbEmailServerRequireSsl.UseVisualStyleBackColor = true;
            // 
            // cbEmailAuthRequired
            // 
            this.cbEmailAuthRequired.AutoSize = true;
            this.cbEmailAuthRequired.Location = new System.Drawing.Point(91, 78);
            this.cbEmailAuthRequired.Name = "cbEmailAuthRequired";
            this.cbEmailAuthRequired.Size = new System.Drawing.Size(122, 17);
            this.cbEmailAuthRequired.TabIndex = 3;
            this.cbEmailAuthRequired.Text = "Server requires login";
            this.cbEmailAuthRequired.UseVisualStyleBackColor = true;
            this.cbEmailAuthRequired.CheckedChanged += new System.EventHandler(this.cbEmailAuthRequired_CheckedChanged);
            // 
            // lblToAddressInfo
            // 
            this.lblToAddressInfo.AutoSize = true;
            this.tlpEmailSettings.SetColumnSpan(this.lblToAddressInfo, 2);
            this.lblToAddressInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblToAddressInfo.Location = new System.Drawing.Point(3, 261);
            this.lblToAddressInfo.Name = "lblToAddressInfo";
            this.lblToAddressInfo.Size = new System.Drawing.Size(298, 13);
            this.lblToAddressInfo.TabIndex = 14;
            this.lblToAddressInfo.Text = "* Use \' ; \' (semicolon) or \' , \' (comma) between email addresses";
            this.lblToAddressInfo.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btnTestEmail
            // 
            this.btnTestEmail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTestEmail.Location = new System.Drawing.Point(240, 312);
            this.btnTestEmail.Name = "btnTestEmail";
            this.btnTestEmail.Size = new System.Drawing.Size(110, 23);
            this.btnTestEmail.TabIndex = 2;
            this.btnTestEmail.Text = "Send Test Email";
            this.btnTestEmail.UseVisualStyleBackColor = true;
            this.btnTestEmail.Click += new System.EventHandler(this.btnTestEmail_Click);
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // btnReset
            // 
            this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReset.Location = new System.Drawing.Point(12, 312);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 23);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // EmailNotificationsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.tlpEmailSettings);
            this.Controls.Add(this.cbEmailUseShortFormat);
            this.Controls.Add(this.btnTestEmail);
            this.Name = "EmailNotificationsControl";
            this.Size = new System.Drawing.Size(362, 338);
            this.tlpEmailSettings.ResumeLayout(false);
            this.tlpEmailSettings.PerformLayout();
            this.tlpEmailAuthTable.ResumeLayout(false);
            this.tlpEmailAuthTable.PerformLayout();
            this.tlpEmailServerSettings.ResumeLayout(false);
            this.tlpEmailServerSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbEmailUseShortFormat;
        private System.Windows.Forms.TableLayoutPanel tlpEmailSettings;
        private System.Windows.Forms.Label lblEmailProvider;
        private System.Windows.Forms.Label lblFromAddress;
        private System.Windows.Forms.Label lblToAddress;
        private System.Windows.Forms.ComboBox cbbEMailServerProvider;
        private System.Windows.Forms.TextBox tbFromAddress;
        private System.Windows.Forms.TextBox tbToAddress;
        private System.Windows.Forms.TableLayoutPanel tlpEmailAuthTable;
        private System.Windows.Forms.Label lblEmailPassword;
        private System.Windows.Forms.Label lblEmailUsername;
        private System.Windows.Forms.TextBox tbEmailUsername;
        private System.Windows.Forms.TextBox tbEmailPassword;
        private System.Windows.Forms.Button btnTestEmail;
        private System.Windows.Forms.TableLayoutPanel tlpEmailServerSettings;
        private System.Windows.Forms.Label lblEmailServerAddress;
        private System.Windows.Forms.TextBox tbEmailServerAddress;
        private System.Windows.Forms.Label lblPortNumber;
        private System.Windows.Forms.TextBox tbEmailPort;
        private System.Windows.Forms.CheckBox cbEmailServerRequireSsl;
        private System.Windows.Forms.CheckBox cbEmailAuthRequired;
        private System.Windows.Forms.Label lblToAddressInfo;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.Button btnReset;
    }
}
