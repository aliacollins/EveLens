// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    public sealed class SerializableESIKey
    {
        private readonly Collection<SerializableCharacterIdentity> m_ignoreList;

        public SerializableESIKey()
        {
            m_ignoreList = new Collection<SerializableCharacterIdentity>();
        }

        [XmlAttribute("id")]
        public long ID { get; set; }

        [XmlAttribute("refreshToken")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Legacy bitflag access mask. Kept for backward compatibility with old settings files.
        /// Use <see cref="AuthorizedScopes"/> instead.
        /// </summary>
        [Obsolete("Use AuthorizedScopes instead. AccessMask is retained only for backward-compatible deserialization.")]
        [XmlAttribute("accessMask")]
        public ulong AccessMask { get; set; }

        [XmlAttribute("monitored")]
        public bool Monitored { get; set; }

        /// <summary>
        /// ESI scope strings that were granted when this key was authenticated.
        /// </summary>
        [XmlArray("authorizedScopes")]
        [XmlArrayItem("scope")]
        public List<string> AuthorizedScopes { get; set; } = new();
    }
}
