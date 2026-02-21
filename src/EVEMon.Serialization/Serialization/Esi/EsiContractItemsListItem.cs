// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiContractItemsListItem
    {
        [DataMember(Name = "record_id")]
        public long RecordID { get; set; }

        [DataMember(Name = "type_id")]
        public int TypeID { get; set; }

        // Max stack size is int32
        [DataMember(Name = "quantity")]
        public int Quantity { get; set; }

        // -1 is singleton or BPO, -2 is blueprint copy
        [DataMember(Name = "raw_quantity")]
        public int RawQuantity { get; set; }

        [DataMember(Name = "is_singleton")]
        public bool Singleton { get; set; }

        [DataMember(Name = "is_included")]
        public bool Included { get; set; }
    }
}
