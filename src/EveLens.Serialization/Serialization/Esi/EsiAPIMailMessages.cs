// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Eve;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [CollectionDataContract]
    public sealed class EsiAPIMailMessages : List<EsiMailMessagesListItem>
    {
        public SerializableAPIMailMessages ToXMLItem()
        {
            var ret = new SerializableAPIMailMessages();
            foreach (var mail in this)
                ret.Messages.Add(mail.ToXMLItem());
            return ret;
        }
    }
}
