// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableAPIContracts
    {
        private readonly Collection<SerializableContractListItem> m_contracts;

        public SerializableAPIContracts()
        {
            m_contracts = new Collection<SerializableContractListItem>();
        }

        [XmlArray("contracts")]
        [XmlArrayItem("contract")]
        public Collection<SerializableContractListItem> Contracts => m_contracts;
    }
}
