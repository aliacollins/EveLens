// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.Helpers;
using EVEMon.Common.Services;
using EVEMon.Core.Events;

namespace EVEMon.BlankCharacter
{
    public partial class BlankCharacterWindow : EVEMonForm
    {
        private IDisposable? _fiveSecondTickSub;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlankCharacterWindow"/> class.
        /// </summary>
        public BlankCharacterWindow()
        {
            InitializeComponent();
        }


        #endregion


        #region Inherited Event Handlers

        /// <summary>
        /// Handles the Load event of the BlankCharacterWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void BlankCharacterWindow_Load(object? sender, EventArgs e)
        {
            _fiveSecondTickSub = AppServices.EventAggregator?.Subscribe<FiveSecondTickEvent>(e => EveMonClient_TimerTick(null, EventArgs.Empty));
            Disposed += OnDisposed;

            buttonOK.Text = "Create";
            buttonOK.Enabled = false;
        }

        /// <summary>
        /// Called when the instance get disposed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnDisposed(object? sender, EventArgs e)
        {
            _fiveSecondTickSub?.Dispose();
            _fiveSecondTickSub = null;
            Disposed -= OnDisposed;
        }

        #endregion


        #region Global Event Handlers

        /// <summary>
        /// Handles the TimerTick event of the EveMonClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_TimerTick(object? sender, EventArgs e)
        {
            buttonOK.Enabled = !string.IsNullOrEmpty(BlankCharacterUIHelper.CharacterName);
            AcceptButton = buttonOK.Enabled ? buttonOK : buttonCancel;
        }

        #endregion


        #region Control Handlers

        /// <summary>
        /// Handles the Click event of the buttonOK control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonOK_Click(object? sender, EventArgs e)
        {
            BlankCharacterUIHelper.AddBlankCharacter();
            Close();
        }

        /// <summary>
        /// Handles the Click event of the buttonCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonCancel_Click(object? sender, EventArgs e)
        {
            Close();
        }

        #endregion
    }
}