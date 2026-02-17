using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.Enumerations;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
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
