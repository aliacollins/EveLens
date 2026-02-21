// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character research points list.
    /// Research points list has no grouping enum — uses a simple None/Agent grouping.
    /// </summary>
    public sealed class ResearchPointsListViewModel : ListViewModel<ResearchPoint, ResearchColumn, ResearchPointsGrouping>
    {
        public ResearchPointsListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterResearchPointsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
        }

        public ResearchPointsListViewModel() : base()
        {
            SubscribeForCharacter<CharacterResearchPointsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
        }

        protected override IEnumerable<ResearchPoint> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<ResearchPoint>();

            return ccp.ResearchPoints;
        }

        protected override bool MatchesFilter(ResearchPoint x, string filter)
        {
            return x.AgentName.Contains(filter, ignoreCase: true) ||
                   x.Field.Contains(filter, ignoreCase: true) ||
                   x.Station.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Constellation.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Constellation.Region.Name.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(ResearchPoint x, ResearchPoint y, ResearchColumn column)
        {
            return column switch
            {
                ResearchColumn.Agent => string.Compare(x.AgentName, y.AgentName, StringComparison.OrdinalIgnoreCase),
                ResearchColumn.Level => x.AgentLevel.CompareTo(y.AgentLevel),
                ResearchColumn.Field => string.Compare(x.Field, y.Field, StringComparison.OrdinalIgnoreCase),
                ResearchColumn.CurrentRP => x.CurrentRP.CompareTo(y.CurrentRP),
                ResearchColumn.PointsPerDay => x.PointsPerDay.CompareTo(y.PointsPerDay),
                ResearchColumn.StartDate => x.StartDate.CompareTo(y.StartDate),
                ResearchColumn.Station => string.Compare(x.Station.Name, y.Station.Name, StringComparison.OrdinalIgnoreCase),
                ResearchColumn.SolarSystem => string.Compare(x.Station.SolarSystemChecked.Name, y.Station.SolarSystemChecked.Name, StringComparison.OrdinalIgnoreCase),
                ResearchColumn.Region => string.Compare(x.Station.SolarSystemChecked.Constellation.Region.Name, y.Station.SolarSystemChecked.Constellation.Region.Name, StringComparison.OrdinalIgnoreCase),
                _ => 0
            };
        }

        protected override string GetGroupKey(ResearchPoint item, ResearchPointsGrouping grouping)
        {
            return grouping switch
            {
                ResearchPointsGrouping.Agent => item.AgentName,
                ResearchPointsGrouping.Field => item.Field,
                ResearchPointsGrouping.Location => item.Station.Name,
                _ => string.Empty
            };
        }
    }

    /// <summary>
    /// Grouping options for the research points list.
    /// The existing codebase does not have a dedicated grouping enum for research points,
    /// so we define a minimal one here.
    /// </summary>
    public enum ResearchPointsGrouping
    {
        None = 0,
        Agent = 1,
        Field = 2,
        Location = 3
    }
}
