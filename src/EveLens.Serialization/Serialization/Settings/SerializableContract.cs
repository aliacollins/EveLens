// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Enumerations;

namespace EveLens.Common.Serialization.Settings
{
    public sealed class SerializableContract
    {
        public SerializableContract()
        {
            ContractType = ContractType.None;
        }

        [XmlAttribute("contractID")]
        public long ContractID { get; set; }

        [XmlAttribute("contractState")]
        public ContractState ContractState { get; set; }

        [XmlAttribute("contractType")]
        public ContractType ContractType { get; set; }

        [XmlAttribute("issuedFor")]
        public IssuedFor IssuedFor { get; set; }
    }
}
