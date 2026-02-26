// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the plan cost card showing ISK costs and injector estimates.
    /// Updated by <see cref="PlanDashboardViewModel"/>.
    /// </summary>
    public sealed class PlanCostCardViewModel : ViewModelBase
    {
        private long _booksCost;
        private long _notKnownBooksCost;
        private long _totalMissingSP;
        private long _characterTotalSP;
        private long _estimatedInjectorPrice = 900_000_000;

        public PlanCostCardViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public PlanCostCardViewModel()
        {
        }

        public long BooksCost
        {
            get => _booksCost;
            set
            {
                if (SetProperty(ref _booksCost, value))
                    OnPropertyChanged(nameof(BooksCostText));
            }
        }

        public string BooksCostText => _booksCost.ToString("N0") + " ISK";

        public long NotKnownBooksCost
        {
            get => _notKnownBooksCost;
            set
            {
                if (SetProperty(ref _notKnownBooksCost, value))
                    OnPropertyChanged(nameof(NotKnownBooksCostText));
            }
        }

        public string NotKnownBooksCostText => _notKnownBooksCost.ToString("N0") + " ISK";

        public long TotalMissingSP
        {
            get => _totalMissingSP;
            set
            {
                if (SetProperty(ref _totalMissingSP, value))
                {
                    OnPropertyChanged(nameof(InjectorCount));
                    OnPropertyChanged(nameof(InjectorCostEstimate));
                    OnPropertyChanged(nameof(InjectorCostText));
                }
            }
        }

        /// <summary>
        /// Gets or sets the character's current total skill points (used for injector bracket calculation).
        /// </summary>
        public long CharacterTotalSP
        {
            get => _characterTotalSP;
            set
            {
                if (SetProperty(ref _characterTotalSP, value))
                {
                    OnPropertyChanged(nameof(InjectorCount));
                    OnPropertyChanged(nameof(InjectorCostEstimate));
                    OnPropertyChanged(nameof(InjectorCostText));
                }
            }
        }

        public long EstimatedInjectorPrice
        {
            get => _estimatedInjectorPrice;
            set
            {
                if (SetProperty(ref _estimatedInjectorPrice, value))
                {
                    OnPropertyChanged(nameof(InjectorCostEstimate));
                    OnPropertyChanged(nameof(InjectorCostText));
                }
            }
        }

        public int InjectorCount
        {
            get
            {
                if (_totalMissingSP <= 0)
                    return 0;

                int spPerInjector = GetSpPerInjector(_characterTotalSP);
                return (int)Math.Ceiling((double)_totalMissingSP / spPerInjector);
            }
        }

        public long InjectorCostEstimate => (long)InjectorCount * _estimatedInjectorPrice;

        public string InjectorCostText
        {
            get
            {
                int count = InjectorCount;
                if (count <= 0)
                    return string.Empty;

                return $"~{count} injectors (est. {InjectorCostEstimate:N0} ISK)";
            }
        }

        /// <summary>
        /// Returns the SP gained per large skill injector based on the character's total SP.
        /// </summary>
        internal static int GetSpPerInjector(long characterSP) => characterSP switch
        {
            < 5_000_000 => 500_000,
            < 50_000_000 => 400_000,
            < 80_000_000 => 300_000,
            _ => 150_000
        };
    }
}
