// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;
using EveLens.Common.Collections;
using EveLens.Common.Models;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a certificate category.
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class CertificateCategoryCollection : ReadonlyKeyedCollection<int, CertificateGroup>
    {
        /// <summary>
        /// Constructor for the character initialization.
        /// </summary>
        /// <param name="character"></param>
        internal CertificateCategoryCollection(Character character)
        {
            if (StaticCertificates.AllGroups == null)
                return;

            foreach (var srcCategory in StaticCertificates.AllGroups)
            {
                var category = new CertificateGroup(character, srcCategory);
                Items[category.ID] = category;
            }
        }
    }
}
