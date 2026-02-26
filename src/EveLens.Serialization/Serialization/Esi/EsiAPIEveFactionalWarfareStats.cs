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
    public sealed class EsiAPIEveFactionalWarfareStats : List<EsiEveFactionalWarfareStatsListItem>
    {
        public SerializableAPIEveFactionalWarfareStats ToXMLItem(EsiAPIEveFactionWars wars)
        {
            var totals = new SerializableEveFacWarfareTotals();
            var ret = new SerializableAPIEveFactionalWarfareStats()
            {
                Totals = totals
            };

            // Manually compute the totals and convert individual war counts
            foreach (var warStats in this)
            {
                var kills = warStats.Kills;
                var vp = warStats.VictoryPoints;

                totals.KillsLastWeek += kills?.LastWeek ?? 0;
                totals.KillsTotal += kills?.Total ?? 0;
                totals.KillsYesterday += kills?.Yesterday ?? 0;

                totals.VictoryPointsLastWeek += vp?.LastWeek ?? 0;
                totals.VictoryPointsTotal += vp?.Total ?? 0;
                totals.VictoryPointsYesterday += vp?.Yesterday ?? 0;

                ret.FactionalWarfareStats.Add(warStats.ToXMLItem());
            }

            // Add the war declarations
            foreach (var war in wars)
                ret.FactionWars.Add(war.ToXMLItem());

            return ret;
        }
    }
}
