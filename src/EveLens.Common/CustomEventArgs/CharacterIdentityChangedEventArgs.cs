// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Models;

namespace EveLens.Common.CustomEventArgs
{
    public sealed class CharacterIdentityChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="identity"></param>
        public CharacterIdentityChangedEventArgs(CharacterIdentity identity)
        {
            CharacterIdentity = identity;
        }

        /// <summary>
        /// Gets the character identity related to this event.
        /// </summary>
        public CharacterIdentity CharacterIdentity { get; }
    }
}