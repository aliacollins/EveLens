// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.LogitechG15
{
    internal sealed class CycleEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CycleEventArgs"/> class.
        /// </summary>
        /// <param name="cycle">if set to <c>true</c> [cycle].</param>
        internal CycleEventArgs(bool cycle)
        {
            Cycle = cycle;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="CycleEventArgs"/> is cycle.
        /// </summary>
        /// <value><c>true</c> if cycle; otherwise, <c>false</c>.</value>
        internal bool Cycle { get; private set; }
    }
}