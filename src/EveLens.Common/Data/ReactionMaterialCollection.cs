// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Collections;
using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    public sealed class ReactionMaterialCollection : ReadonlyCollection<SerializableReactionInfo>
    {       
        /// <summary>
        /// Initializes a new instance of the <see cref="ReactionMaterialCollection"/> class.
        /// </summary>
        /// <param name="reactionInfo">The reactionInfo.</param>
        internal ReactionMaterialCollection(ICollection<SerializableReactionInfo> reactionInfo)
            : base(reactionInfo?.Count ?? 0)
        {
            if (reactionInfo == null)
                return;

            foreach (SerializableReactionInfo reaction in reactionInfo)
            {
                Items.Add(reaction);
            }
        }
    }
}