// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations.CCPAPI;

namespace EVEMon.Common.Serialization.Settings
{
    public sealed class SerializableAccount
    {
        public SerializableAccount()
        {
            IgnoreList = new List<SerializableCharacterIdentity>();
        }

        [XmlAttribute("id")]
        public long ID { get; set; }

        [XmlAttribute("key")]
        public string? Key { get; set; }

        [XmlAttribute("keyLevel")]
        public CredentialsLevel KeyLevel { get; set; }

        [XmlElement("paidUntil")]
        public DateTime PaidUntil { get; set; }

        [XmlElement("createDate")]
        public DateTime CreateDate { get; set; }

        [XmlElement("lastAccountStatusUpdate")]
        public DateTime LastAccountStatusUpdate { get; set; }

        [XmlElement("lastCharacterListUpdate")]
        public DateTime LastCharacterListUpdate { get; set; }

        [XmlElement("ignored")]
        public List<SerializableCharacterIdentity> IgnoreList { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        internal SerializableAccount Clone()
        {
            SerializableAccount clone = new SerializableAccount
                                            {
                                                ID = ID,
                                                Key = Key,
                                                KeyLevel = KeyLevel,
                                                PaidUntil = PaidUntil,
                                                CreateDate = CreateDate,
                                                LastAccountStatusUpdate = LastAccountStatusUpdate,
                                                LastCharacterListUpdate = LastCharacterListUpdate
                                            };
            clone.IgnoreList.AddRange(IgnoreList.Select(x => x.Clone()));
            return clone;
        }
    }
}