// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EveLens.Common.Constants;

namespace EveLens.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a serializable version of an API provider. Used for settings persistence.
    /// </summary>
    public sealed class SerializableAPIProvider
    {
        private readonly Collection<SerializableAPIMethod> m_methods;

        public SerializableAPIProvider()
        {
            Name = "New provider";
            Address = NetworkConstants.ESIBase;
            SupportsCompressedResponse = false;
            m_methods = new Collection<SerializableAPIMethod>();
        }

        [XmlAttribute("supportsCompressedResponses")]
        public bool SupportsCompressedResponse { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("url")]
        public string Address { get; set; }

        [XmlArray("methods")]
        [XmlArrayItem("method")]
        public Collection<SerializableAPIMethod> Methods => m_methods;
    }
}
