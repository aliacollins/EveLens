// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Constants;

namespace EVEMon.Common.Serialization.Osmium.Loadout
{
    public sealed class SerializableOsmiumLoadoutFeed
    {
        public long ID => long.Parse(Uri?.Replace($"{NetworkConstants.OsmiumBaseUrl}/loadout/", string.Empty) ?? "0",
            CultureConstants.InvariantCulture);

        public string? Uri { get; set; }

        public string? Name { get; set; }

        public int ShipTypeID { get; set; }

        public string? ShipTypeName { get; set; }

        public SerializableOsmiumLoadoutAuthor? Author { get; set; }

        public long CreationDate { get; set; }

        public string? RawDescription { get; set; }

        public int UpVotes { get; set; }

        public int DownVotes { get; set; }

        public int Rating => UpVotes - DownVotes;
    }
}
