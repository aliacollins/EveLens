// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.Models;

namespace EveLens.Common.CustomEventArgs
{
    public sealed class ContractsEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="endedContracts">The ended contracts.</param>
        public ContractsEventArgs(Character character, IEnumerable<Contract> endedContracts)
        {
            Character = character;
            EndedContracts = endedContracts;
        }

        /// <summary>
        /// Gets the character.
        /// </summary>
        /// <value>The character.</value>
        public Character Character { get; }

        /// <summary>
        /// Gets the ended contracts.
        /// </summary>
        /// <value>The ended contracts.</value>
        public IEnumerable<Contract> EndedContracts { get; }
    }
}
