// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Events;
using EveLens.Common.Extensions;
using EveLens.Common.Models;
using EveLens.Common.SettingsObjects;
using EveLens.Common.Enumerations;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character industry jobs list.
    /// </summary>
    public sealed class IndustryJobsListViewModel : ListViewModel<IndustryJob, IndustryJobColumn, IndustryJobGrouping>
    {
        private bool _hideInactive;
        private IssuedFor _showIssuedFor = IssuedFor.All;

        public IndustryJobsListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<IndustryJobsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
        }

        public IndustryJobsListViewModel() : base()
        {
            SubscribeForCharacter<IndustryJobsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
        }

        public bool HideInactive
        {
            get => _hideInactive;
            set { if (SetProperty(ref _hideInactive, value)) Refresh(); }
        }

        public IssuedFor ShowIssuedFor
        {
            get => _showIssuedFor;
            set { if (SetProperty(ref _showIssuedFor, value)) Refresh(); }
        }

        protected override IEnumerable<IndustryJob> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<IndustryJob>();

            IEnumerable<IndustryJob> jobs = ccp.IndustryJobs
                .Where(x => x.InstalledItem != null && x.OutputItem != null && x.SolarSystem != null);

            if (_hideInactive)
                jobs = jobs.Where(x => x.IsActive);

            if (_showIssuedFor != IssuedFor.All)
                jobs = jobs.Where(x => x.IssuedFor == _showIssuedFor);

            return jobs;
        }

        protected override bool MatchesFilter(IndustryJob x, string filter)
        {
            return x.InstalledItem.Name.Contains(filter, ignoreCase: true) ||
                   x.OutputItem.Name.Contains(filter, ignoreCase: true) ||
                   (x.Installation?.Contains(filter, ignoreCase: true) ?? false) ||
                   (x.SolarSystem?.Name?.Contains(filter, ignoreCase: true) ?? false) ||
                   (x.SolarSystem?.Constellation?.Name?.Contains(filter, ignoreCase: true) ?? false) ||
                   (x.SolarSystem?.Constellation?.Region?.Name?.Contains(filter, ignoreCase: true) ?? false);
        }

        protected override int CompareItems(IndustryJob x, IndustryJob y, IndustryJobColumn column)
        {
            return column switch
            {
                IndustryJobColumn.State => x.State.CompareTo(y.State),
                IndustryJobColumn.TTC => x.EndDate.CompareTo(y.EndDate),
                IndustryJobColumn.InstalledItem => string.Compare(x.InstalledItem.Name, y.InstalledItem.Name, StringComparison.OrdinalIgnoreCase),
                IndustryJobColumn.OutputItem => string.Compare(x.OutputItem.Name, y.OutputItem.Name, StringComparison.OrdinalIgnoreCase),
                IndustryJobColumn.InstallTime => x.InstalledTime.CompareTo(y.InstalledTime),
                IndustryJobColumn.EndTime => x.EndDate.CompareTo(y.EndDate),
                IndustryJobColumn.Cost => x.Cost.CompareTo(y.Cost),
                IndustryJobColumn.Runs => x.Runs.CompareTo(y.Runs),
                _ => 0
            };
        }

        protected override string GetGroupKey(IndustryJob item, IndustryJobGrouping grouping)
        {
            return grouping switch
            {
                IndustryJobGrouping.State or IndustryJobGrouping.StateDesc => item.State.ToString(),
                IndustryJobGrouping.EndDate or IndustryJobGrouping.EndDateDesc => item.EndDate.ToShortDateString(),
                IndustryJobGrouping.InstalledItemType or IndustryJobGrouping.InstalledItemTypeDesc => item.InstalledItem.GroupName,
                IndustryJobGrouping.OutputItemType or IndustryJobGrouping.OutputItemTypeDesc => item.OutputItem.GroupName,
                IndustryJobGrouping.Activity or IndustryJobGrouping.ActivityDesc => item.Activity.GetDescription(),
                IndustryJobGrouping.Location or IndustryJobGrouping.LocationDesc => item.Installation ?? string.Empty,
                _ => string.Empty
            };
        }
    }
}
