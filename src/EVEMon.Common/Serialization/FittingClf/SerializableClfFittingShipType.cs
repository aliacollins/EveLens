// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;
using EVEMon.Common.Data;

namespace EVEMon.Common.Serialization.FittingClf
{
    [DataContract]
    public sealed class SerializableClfFittingShipType
    {
        [DataMember(Name = "typeid")]
        public int TypeID
        {
            get { return Item?.ID ?? 0; }
            set { Item = StaticItems.GetItemByID(value) ?? Item.UnknownItem; }
        }

        public Item? Item { get; set; }
    }
}