// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Models;

namespace EVEMon.Common.CustomEventArgs
{
    public sealed class ESIKeyInfoChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="esiKey"></param>
        public ESIKeyInfoChangedEventArgs(ESIKey esiKey)
        {
            ESIKey = esiKey;
        }

        /// <summary>
        /// Gets the ESI key related to this event.
        /// </summary>
        public ESIKey ESIKey { get; }
    }
}
