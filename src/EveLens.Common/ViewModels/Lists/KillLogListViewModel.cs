// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations;
using EveLens.Common.Events;
using EveLens.Common.Extensions;
using EveLens.Common.Models;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character kill log list.
    /// The original kill log control uses column-index-based sorting with no dedicated column/grouping enums,
    /// so we define minimal enums here.
    /// </summary>
    public sealed class KillLogListViewModel : ListViewModel<KillLog, KillLogColumn, KillLogGrouping>
    {
        private int _killCount;
        private int _lossCount;

        public KillLogListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterKillLogUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
        }

        public KillLogListViewModel() : base()
        {
            SubscribeForCharacter<CharacterKillLogUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
        }

        /// <summary>
        /// Gets the number of kills in the current filtered set.
        /// </summary>
        public int KillCount
        {
            get => _killCount;
            private set => SetProperty(ref _killCount, value);
        }

        /// <summary>
        /// Gets the number of losses in the current filtered set.
        /// </summary>
        public int LossCount
        {
            get => _lossCount;
            private set => SetProperty(ref _lossCount, value);
        }

        protected override IEnumerable<KillLog> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<KillLog>();

            return ccp.KillLog;
        }

        protected override bool MatchesFilter(KillLog x, string filter)
        {
            return x.Victim.ShipTypeName.Contains(filter, ignoreCase: true) ||
                   x.Victim.Name.Contains(filter, ignoreCase: true) ||
                   x.Victim.CorporationName.Contains(filter, ignoreCase: true) ||
                   x.Victim.AllianceName.Contains(filter, ignoreCase: true) ||
                   x.Victim.FactionName.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(KillLog x, KillLog y, KillLogColumn column)
        {
            return column switch
            {
                KillLogColumn.KillTime => x.KillTime.CompareTo(y.KillTime),
                KillLogColumn.ShipType => string.Compare(x.Victim.ShipTypeName, y.Victim.ShipTypeName, StringComparison.OrdinalIgnoreCase),
                KillLogColumn.VictimName => string.Compare(x.Victim.Name, y.Victim.Name, StringComparison.OrdinalIgnoreCase),
                KillLogColumn.Corporation => string.Compare(x.Victim.CorporationName, y.Victim.CorporationName, StringComparison.OrdinalIgnoreCase),
                KillLogColumn.Alliance => string.Compare(x.Victim.AllianceName, y.Victim.AllianceName, StringComparison.OrdinalIgnoreCase),
                KillLogColumn.Faction => string.Compare(x.Victim.FactionName, y.Victim.FactionName, StringComparison.OrdinalIgnoreCase),
                _ => 0
            };
        }

        protected override string GetGroupKey(KillLog item, KillLogGrouping grouping)
        {
            return grouping switch
            {
                KillLogGrouping.KillsVsLosses => item.Group == KillGroup.Kills ? "Kills" : "Losses",
                KillLogGrouping.Date => item.KillTime.ToShortDateString(),
                KillLogGrouping.ShipType => item.Victim.ShipTypeName,
                KillLogGrouping.Corporation => item.Victim.CorporationName,
                _ => string.Empty
            };
        }

        protected override DateTime GetItemTimestamp(KillLog item) => item.KillTime;

        /// <summary>
        /// Determines whether the given kill log entry is a loss for the current character.
        /// </summary>
        public bool IsLoss(KillLog item) => item.Group == KillGroup.Losses;

        /// <summary>
        /// Updates kill/loss counts from the current Items collection.
        /// Call after Refresh() has been invoked (e.g. by setting Grouping or TextFilter).
        /// </summary>
        public void UpdateCounts()
        {
            int kills = 0;
            int losses = 0;
            foreach (var item in Items)
            {
                if (item.Group == KillGroup.Kills)
                    kills++;
                else
                    losses++;
            }
            KillCount = kills;
            LossCount = losses;
        }
    }

    /// <summary>
    /// Column enum for kill log sorting. Maps to the column-index-based sorting in the original control.
    /// </summary>
    public enum KillLogColumn
    {
        None = -1,
        KillTime = 0,
        ShipType = 1,
        VictimName = 2,
        Corporation = 3,
        Alliance = 4,
        Faction = 5
    }

    /// <summary>
    /// Grouping options for the kill log list.
    /// </summary>
    public enum KillLogGrouping
    {
        None = 0,
        KillsVsLosses = 1,
        Date = 2,
        ShipType = 3,
        Corporation = 4
    }
}
