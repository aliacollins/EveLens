// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Fuzzworks
{
    [DataContract]
    public sealed class SerializableFuzzworksPriceItem
    {
        [DataMember(Name = "buy")]
        public SerializableFuzzworksPriceListItem? Buy { get; set; }

        [DataMember(Name = "sell")]
        public SerializableFuzzworksPriceListItem? Sell { get; set; }
    }
}
