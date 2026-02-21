// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Serialization.Esi;

namespace EVEMon.Common.Models
{
    public sealed class EveMailingList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EveMailingList"/> class.
        /// </summary>
        /// <param name="src">The SRC.</param>
        internal EveMailingList(EsiMailingListsListItem src)
        {
            ID = src.ID;
            Name = src.DisplayName;
        }

        /// <summary>
        /// Gets the ID.
        /// </summary>
        /// <value>
        /// The ID.
        /// </value>
        internal long ID { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        internal string Name { get; }
    }
}
