// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// My Hangar's data: the character's ship assets grouped by STATION — you browse
    /// one hangar at a time, the way a capsuleer actually thinks ("my Jita ships"),
    /// which also caps the scene naturally (a station holds a handful of ships; the
    /// 400-ship account is spread across New Eden). Selecting a ship berths its
    /// plain hull on the bay pad, viewed from the balcony.
    /// </summary>
    public sealed class SkinrGarageViewModel : ViewModelBase
    {
        /// <summary>The character whose hangars we're walking.</summary>
        public Character? Character { get; set; }

        /// <summary>
        /// Stations holding at least one of the character's ships, busiest first —
        /// the station selector's contents. Location names arrive resolved by the
        /// asset pipeline ("Jita IV - Moon 4 - Caldari Navy Assembly Plant").
        /// </summary>
        public IReadOnlyList<GarageStation> Stations()
        {
            return ShipAssets()
                .GroupBy(a => StationName(a))
                .Select(g => new GarageStation(g.Key, (int)g.Sum(a => Math.Max(1, a.Quantity))))
                .OrderByDescending(s => s.Ships)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>The distinct hulls parked at a station, largest hull name groups
        /// intact (5 Rifters = one entry, count 5).</summary>
        public IReadOnlyList<GarageShip> ShipsAt(string stationName)
        {
            return ShipAssets()
                .Where(a => StationName(a) == stationName)
                .GroupBy(a => a.Item!.ID)
                .Select(g => new GarageShip(
                    g.Key,
                    g.First().Item!.LocalizedName,
                    (int)g.Sum(a => Math.Max(1, a.Quantity))))
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// A plain-hull "design" for a ship type: no layout, so the resolver produces
        /// the base hull with zero coatings — exactly what sits in a real hangar
        /// (ESI does not expose which skin a stored ship wears).
        /// </summary>
        public static EsiSkinrRecipe HullRecipe(int typeId, string name) => new()
        {
            Id = "hull:" + typeId,
            Name = name,
            ShipTypeId = typeId
        };

        private IEnumerable<Asset> ShipAssets()
        {
            if (Character is not CCPCharacter ccp)
                return Enumerable.Empty<Asset>();
            return ccp.Assets.Where(a =>
                a.Item != null && a.Item.CategoryName == "Ship");
        }

        private static string StationName(Asset asset)
        {
            string name = asset.FullLocation;
            return string.IsNullOrEmpty(name) ? "Unknown location" : name;
        }
    }

    /// <summary>One station in the selector: its resolved name and ship count.</summary>
    public sealed record GarageStation(string Name, int Ships);

    /// <summary>One hull group at a station.</summary>
    public sealed record GarageShip(int TypeId, string Name, int Count);
}
