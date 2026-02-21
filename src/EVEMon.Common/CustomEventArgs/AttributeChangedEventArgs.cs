// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Models;

namespace EVEMon.Common.CustomEventArgs
{
    public sealed class AttributeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="remapping">The remapping.</param>
        public AttributeChangedEventArgs(RemappingResult remapping)
        {
            Remapping = remapping;
        }

        /// <summary>
        /// Gets the remapping.
        /// </summary>
        /// <value>The remapping.</value>
        public RemappingResult Remapping { get; }
    }
}
