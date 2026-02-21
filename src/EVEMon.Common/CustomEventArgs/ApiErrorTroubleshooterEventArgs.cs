// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Controls;

namespace EVEMon.Common.CustomEventArgs
{
    /// <summary>
    /// Provides event arguments for API Error troubleshooters to notify their parents.
    /// </summary>
    public sealed class ApiErrorTroubleshooterEventArgs : EventArgs
    {
        private readonly bool m_resolved;
        private readonly ResolutionAction m_action;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiErrorTroubleshooterEventArgs"/> class with the error unresolved.
        /// </summary>
        public ApiErrorTroubleshooterEventArgs()
        {
            m_action = ResolutionAction.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiErrorTroubleshooterEventArgs"/> class with the error resolved.
        /// </summary>
        /// <param name="action">The action.</param>
        public ApiErrorTroubleshooterEventArgs(ResolutionAction action)
        {
            m_resolved = true;
            m_action = action;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ApiErrorTroubleshooterEventArgs"/> is resolved.
        /// </summary>
        /// <value><c>true</c> if resolved; otherwise, <c>false</c>.</value>
        public bool Resolved => m_resolved;

        /// <summary>
        /// Gets the action.
        /// </summary>
        /// <value>The action.</value>
        public ResolutionAction Action => m_action;
    }
}