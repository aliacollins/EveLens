// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.Factories;

namespace EVEMon.NotificationWindow
{
    /// <summary>
    /// Notification dialog to inform user of a possible clock error
    /// </summary>
    public partial class TimeCheckNotification : EVEMonForm
    {
        private readonly DateTime m_serverTime;
        private readonly DateTime m_localTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeCheckNotification"/> class.
        /// </summary>
        private TimeCheckNotification()
        {
            InitializeComponent();
            uxTitleLabel.Font = FontFactory.GetFont("Tahoma", 12F);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeCheckNotification"/> class.
        /// </summary>
        /// <param name="serverTime">The server time.</param>
        /// <param name="localTime">The local time.</param>
        public TimeCheckNotification(DateTime serverTime, DateTime localTime)
            : this()
        {
            m_serverTime = serverTime;
            m_localTime = localTime;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.Shown"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            uxTimeZoneField.Text = TimeZone.CurrentTimeZone.StandardName;
            uxExpectedTimeField.Text = $"{m_serverTime}";
            uxActualTimeField.Text = $"{m_localTime}";
            uxCheckTimeOnStartUpCheckBox.Checked = Settings.Updates.CheckTimeOnStartup;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.Closing"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"/> that contains the event data.</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            Settings.Updates.CheckTimeOnStartup = uxCheckTimeOnStartUpCheckBox.Checked;
        }

        /// <summary>
        /// Handles the Click event of the uxCloseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void uxCloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}