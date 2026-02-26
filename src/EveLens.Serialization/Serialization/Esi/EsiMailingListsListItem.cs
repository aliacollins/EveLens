// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiMailingListsListItem
    {
        [DataMember(Name = "mailing_list_id")]
        public long ID { get; set; }

        [DataMember(Name = "name")]
        public string? DisplayName { get; set; }
    }
}
