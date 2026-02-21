// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiMailRecipientListItem
    {
        [DataMember(Name = "recipient_id")]
        public long RecipientID { get; set; }

        // One of: alliance, character, corporation, mailing_list
        [DataMember(Name = "recipient_type")]
        public string? RecipientType { get; set; }
    }
}
