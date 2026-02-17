using System;
using System.Collections.Generic;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character kill log list.
    /// The original kill log control uses column-index-based sorting with no dedicated column/grouping enums,
    /// so we define minimal enums here.
    /// </summary>
    public sealed class KillLogListViewModel : ListViewModel<KillLog, KillLogColumn, KillLogGrouping>
    {
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
                KillLogGrouping.Date => item.KillTime.ToShortDateString(),
                KillLogGrouping.ShipType => item.Victim.ShipTypeName,
                KillLogGrouping.Corporation => item.Victim.CorporationName,
                _ => string.Empty
            };
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
        Date = 1,
        ShipType = 2,
        Corporation = 3
    }
}
