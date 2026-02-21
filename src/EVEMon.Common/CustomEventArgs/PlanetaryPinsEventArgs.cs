// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EVEMon.Common.Models;

namespace EVEMon.Common.CustomEventArgs
{
    public sealed class PlanetaryPinsEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="planetaryPins">The planetary pins.</param>
        public PlanetaryPinsEventArgs(Character character, IEnumerable<PlanetaryPin> planetaryPins)
        {
            Character = character;
            CompletedPins = planetaryPins.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the character related to this event.
        /// </summary>
        public Character Character { get; }

        /// <summary>
        /// Gets the planetary pins related to this event.
        /// </summary>
        public ReadOnlyCollection<PlanetaryPin> CompletedPins { get; }
    }
}
