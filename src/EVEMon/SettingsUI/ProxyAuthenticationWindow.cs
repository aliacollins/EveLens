// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.SettingsUI
{
    public partial class ProxyAuthenticationWindow : EVEMonForm
    {
        private readonly ProxySettings m_proxySetting = null!;

        private ProxyAuthenticationWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="settings"></param>
        public ProxyAuthenticationWindow(ProxySettings settings)
            : this()
        {
            m_proxySetting = settings;
            UpdateFields();
        }

        /// <summary>
        /// Updates the fields.
        /// </summary>
        private void UpdateFields()
        {
            if (m_proxySetting == null)
                return;

            switch (m_proxySetting.Authentication)
            {
                case ProxyAuthentication.None:
                    rbNoAuth.Checked = true;
                    break;
                case ProxyAuthentication.SystemDefault:
                    rbSystemDefault.Checked = true;
                    break;
                case ProxyAuthentication.Specified:
                    rbSuppliedAuth.Checked = true;
                    break;
            }

            tlpSpecifiedAuth.Enabled = false;
            tbUsername.Text = m_proxySetting.Username;
            tbPassword.Text = Util.Decrypt(m_proxySetting.Password, m_proxySetting.Username);
        }

        /// <summary>
        /// Handles the CheckedChanged event of the rbSuppliedAuth control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void rbSuppliedAuth_CheckedChanged(object sender, EventArgs e)
        {
            tlpSpecifiedAuth.Enabled = rbSuppliedAuth.Checked;
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the btnOk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (rbNoAuth.Checked)
                m_proxySetting.Authentication = ProxyAuthentication.None;
            else if (rbSystemDefault.Checked)
                m_proxySetting.Authentication = ProxyAuthentication.SystemDefault;
            else if (rbSuppliedAuth.Checked)
                m_proxySetting.Authentication = ProxyAuthentication.Specified;

            m_proxySetting.Username = tbUsername.Text.Trim();
            m_proxySetting.Password = Util.Encrypt(tbPassword.Text.Trim(), tbUsername.Text.Trim());

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}