// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace EVEMon.Common.Controls
{
    public partial class EveFolderWindow : EVEMonForm
    {
        private IEnumerable<string> m_specifiedPortraitFolder = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EveFolderWindow"/> class.
        /// </summary>
        public EveFolderWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Load event of the EVEFolderWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EVEFolderWindow_Load(object sender, EventArgs e)
        {
            if (!EveMonClient.DefaultEvePortraitCacheFolders.Any())
            {
                SpecifyFolderRadioButton.Checked = true;
                DefaultFolderRadioButton.Enabled = false;
            }
            else
                SpecifyFolderRadioButton.Checked = true;
        }

        /// <summary>
        /// Gets the specified EVE portrait cache folder.
        /// </summary>
        /// <value>The specified EVE portrait cache folder.</value>
        public IEnumerable<string> SpecifiedEVEPortraitCacheFolder => m_specifiedPortraitFolder;

        /// <summary>
        /// Handles the Click event of the BrowseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void BrowseButton_Click(object sender, EventArgs e)
        {
            DialogResult dr = OpenDirFolderBrowserDialog.ShowDialog();
            if (dr != DialogResult.OK)
                return;

            FilenameTextBox.Text = OpenDirFolderBrowserDialog.SelectedPath;
            OKButton.Enabled = true;
            AcceptButton = OKButton;
        }

        /// <summary>
        /// Handles the Click event of the OKButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OKButton_Click(object sender, EventArgs e)
        {
            m_specifiedPortraitFolder = DefaultFolderRadioButton.Checked
                                            ? EveMonClient.DefaultEvePortraitCacheFolders
                                            : new List<string> { FilenameTextBox.Text };

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the DefaultFolderRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void DefaultFolderRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (!DefaultFolderRadioButton.Checked)
                return;

            OKButton.Enabled = true;
            AcceptButton = OKButton;
        }

        /// <summary>
        /// Handles the CheckedChanged event of the SpecifyFolderRadioButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void SpecifyFolderRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            BrowseButton.Enabled = FilenameTextBox.Enabled = SpecifyFolderRadioButton.Checked;
            if (SpecifyFolderRadioButton.Checked && FilenameTextBox.Text.Length == 0)
                AcceptButton = BrowseButton;

            OKButton.Enabled = SpecifyFolderRadioButton.Checked && FilenameTextBox.Text.Length != 0;
        }
    }
}