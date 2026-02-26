// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiMailMessagesListItem : EsiMailBase
    {
        [DataMember(Name = "mail_id")]
        public long MessageID { get; set; }

        [DataMember(Name = "is_read", IsRequired = false)]
        public bool Read { get; set; }

        public SerializableMailMessagesListItem ToXMLItem()
        {
            var ret = new SerializableMailMessagesListItem()
            {
                MessageID = MessageID,
                SenderID = SenderID,
                SentDate = SentDate,
                Title = Title,
            };

            // Split up the recipients by type
            if (Recipients != null)
                foreach (var recipient in Recipients)
                {
                    long id = recipient.RecipientID;
                    switch (recipient.RecipientType)
                    {
                    case "corporation":
                    case "alliance":
                        // Alliance
                        ret.ToCorpOrAllianceID = id;
                        break;
                    case "mailing_list":
                        // List
                        ret.ToListID.Add(id);
                        break;
                    case "character":
                        // Character
                        ret.ToCharacterIDs.Add(id);
                        break;
                    default:
                        // Ignore
                        break;
                    }
                }

            return ret;
        }
    }
}
