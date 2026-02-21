// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Controls
{
    public sealed partial class TipWindow : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TipWindow"/> class.
        /// </summary>
        private TipWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TipWindow" /> class.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <param name="title">The title.</param>
        /// <param name="tiptext">The tiptext.</param>
        /// <param name="key">The key.</param>
        /// <param name="checkboxVisible">if set to <c>true</c> the checkbox is visible.</param>
        private TipWindow(Form form, string title, string tiptext, string key, bool checkboxVisible)
            : this()
        {
            form.Controls.Add(this);

            Tag = key;
            cbDontShowAgain.Visible = checkboxVisible;
            pictureBox.Image = SystemIcons.Information.ToBitmap();

            Text = title;
            TipLabel.Text = tiptext;
            // Aligns the top right corner of the tip window with the top right corner of the owner's client rectangle
            Location = new Point(form.ClientRectangle.Left + form.ClientSize.Width - Width,
                form.ClientRectangle.Top);
            Anchor = AnchorStyles.Top | AnchorStyles.Right;
            BringToFront();
        }

        /// <summary>
        /// Handles the Click event of the btnOk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (cbDontShowAgain.Checked)
            {
                Settings.UI.ConfirmedTips.Add((string)Tag);
                Settings.Save();
            }

            Parent.Controls.Remove(this);
            
            Dispose();
        }

        /// <summary>
        /// Show a "tip of the day"-like message on the top right corner of the given window.
        /// </summary>
        /// <param name="form">The owner window.</param>
        /// <param name="key">The key used to store informations about messages the user already saw. Every messages is only displayed once.</param>
        /// <param name="title">The title of the tip window.</param>
        /// <param name="tiptext">The text of the tip window.</param>
        /// <param name="checkBoxVisible">if set to <c>true</c> the checkbox is visible.</param>
        /// <exception cref="System.ArgumentNullException">form</exception>
        public static void ShowTip(Form form, string key, string title, string tiptext, bool checkBoxVisible = true)
        {
            form.ThrowIfNull(nameof(form));

            if (Settings.UI.ConfirmedTips.Contains(key))
                return;

            // Quit if it's already shown
            if (form.Controls.OfType<TipWindow>().Any())
                return;

            // Gets disposed when clicking the OK button
            TipWindow tipWindow = new TipWindow(form, title, tiptext, key, checkBoxVisible);
            tipWindow.Show();
        }
    }
}