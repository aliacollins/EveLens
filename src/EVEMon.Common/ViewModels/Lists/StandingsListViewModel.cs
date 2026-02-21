// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character standings list.
    /// </summary>
    public sealed class StandingsListViewModel : ListViewModel<Standing, StandingColumn, StandingGrouping>
    {
        public StandingsListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterStandingsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
        }

        public StandingsListViewModel() : base()
        {
            SubscribeForCharacter<CharacterStandingsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
        }

        protected override IEnumerable<Standing> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<Standing>();

            return ccp.Standings;
        }

        protected override bool MatchesFilter(Standing x, string filter)
        {
            return x.EntityName.Contains(filter, ignoreCase: true) ||
                   x.Group.ToString().Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(Standing x, Standing y, StandingColumn column)
        {
            return column switch
            {
                StandingColumn.EntityName => string.Compare(x.EntityName, y.EntityName, StringComparison.OrdinalIgnoreCase),
                StandingColumn.StandingValue => x.StandingValue.CompareTo(y.StandingValue),
                StandingColumn.EffectiveStanding => x.EffectiveStanding.CompareTo(y.EffectiveStanding),
                StandingColumn.Group => x.Group.CompareTo(y.Group),
                _ => 0
            };
        }

        protected override string GetGroupKey(Standing item, StandingGrouping grouping)
        {
            return grouping switch
            {
                StandingGrouping.Group => item.Group.ToString(),
                StandingGrouping.Status => Standing.Status(item.StandingValue).ToString(),
                _ => string.Empty
            };
        }
    }
}
