// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Constants;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character planetary colonies list.
    /// </summary>
    public sealed class PlanetaryListViewModel : ListViewModel<PlanetaryPin, PlanetaryColumn, PlanetaryGrouping>
    {
        private bool _showEcuOnly;

        public PlanetaryListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterPlanetaryColoniesUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterPlanetaryLayoutUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
        }

        public PlanetaryListViewModel() : base()
        {
            SubscribeForCharacter<CharacterPlanetaryColoniesUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterPlanetaryLayoutUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
        }

        public bool ShowEcuOnly
        {
            get => _showEcuOnly;
            set { if (SetProperty(ref _showEcuOnly, value)) Refresh(); }
        }

        protected override IEnumerable<PlanetaryPin> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<PlanetaryPin>();

            var pins = new List<PlanetaryPin>();
            foreach (var colony in ccp.PlanetaryColonies)
            {
                pins.AddRange(colony.Pins);
            }

            if (_showEcuOnly)
                return pins.Where(pin => DBConstants.EcuTypeIDs.Any(id => id == pin.TypeID)).ToList();

            return pins;
        }

        protected override bool MatchesFilter(PlanetaryPin x, string filter)
        {
            return x.Colony.PlanetName.Contains(filter, ignoreCase: true) ||
                   x.Colony.PlanetTypeName.Contains(filter, ignoreCase: true) ||
                   x.Colony.SolarSystem.Name.Contains(filter, ignoreCase: true) ||
                   x.Colony.SolarSystem.Constellation.Name.Contains(filter, ignoreCase: true) ||
                   x.Colony.SolarSystem.Constellation.Region.Name.Contains(filter, ignoreCase: true) ||
                   x.TypeName.Contains(filter, ignoreCase: true) ||
                   x.ContentTypeName.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(PlanetaryPin x, PlanetaryPin y, PlanetaryColumn column)
        {
            return column switch
            {
                PlanetaryColumn.State => x.State.CompareTo(y.State),
                PlanetaryColumn.TTC => x.ExpiryTime.CompareTo(y.ExpiryTime),
                PlanetaryColumn.TypeName => string.Compare(x.TypeName, y.TypeName, StringComparison.OrdinalIgnoreCase),
                PlanetaryColumn.InstallTime => x.InstallTime.CompareTo(y.InstallTime),
                PlanetaryColumn.EndTime => x.ExpiryTime.CompareTo(y.ExpiryTime),
                PlanetaryColumn.PlanetTypeName => string.Compare(x.Colony.PlanetTypeName, y.Colony.PlanetTypeName, StringComparison.OrdinalIgnoreCase),
                PlanetaryColumn.PlanetName => string.Compare(x.Colony.PlanetName, y.Colony.PlanetName, StringComparison.OrdinalIgnoreCase),
                PlanetaryColumn.SolarSystem => string.Compare(x.Colony.SolarSystem.Name, y.Colony.SolarSystem.Name, StringComparison.OrdinalIgnoreCase),
                PlanetaryColumn.Region => string.Compare(x.Colony.SolarSystem.Constellation.Region.Name, y.Colony.SolarSystem.Constellation.Region.Name, StringComparison.OrdinalIgnoreCase),
                PlanetaryColumn.ContentTypeName => string.Compare(x.ContentTypeName, y.ContentTypeName, StringComparison.OrdinalIgnoreCase),
                PlanetaryColumn.Quantity => x.ContentQuantity.CompareTo(y.ContentQuantity),
                _ => 0
            };
        }

        protected override string GetGroupKey(PlanetaryPin item, PlanetaryGrouping grouping)
        {
            return grouping switch
            {
                PlanetaryGrouping.Colony or PlanetaryGrouping.ColonyDesc => item.Colony.PlanetName,
                PlanetaryGrouping.SolarSystem or PlanetaryGrouping.SolarSystemDesc => item.Colony.SolarSystem.Name,
                PlanetaryGrouping.PlanetType or PlanetaryGrouping.PlanetTypeDesc => item.Colony.PlanetTypeName,
                PlanetaryGrouping.EndDate or PlanetaryGrouping.EndDateDesc => item.ExpiryTime.ToShortDateString(),
                PlanetaryGrouping.GroupName or PlanetaryGrouping.GroupNameDesc => item.GroupName,
                _ => string.Empty
            };
        }
    }
}
