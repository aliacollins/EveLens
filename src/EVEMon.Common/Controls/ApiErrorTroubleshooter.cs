// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Controls
{
    public partial class ApiErrorTroubleshooter : UserControl
    {
        /// <summary>
        /// Occurs when a resolution has been completed.
        /// </summary>
        public event EventHandler<ApiErrorTroubleshooterEventArgs> ErrorResolved;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiErrorTroubleshooter"/> class.
        /// </summary>
        protected ApiErrorTroubleshooter()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when the error is unresolved.
        /// </summary>
        protected void OnErrorUnresolved()
        {
            ErrorResolved?.ThreadSafeInvoke(null, new ApiErrorTroubleshooterEventArgs());
        }

        /// <summary>
        /// Called when the error is resolved.
        /// </summary>
        /// <param name="action">The action.</param>
        protected void OnErrorResolved(ResolutionAction action)
        {
            ErrorResolved?.ThreadSafeInvoke(null, new ApiErrorTroubleshooterEventArgs(action));
        }
    }
}