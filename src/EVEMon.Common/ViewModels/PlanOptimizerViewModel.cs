using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the attribute optimizer panel.
    /// Runs <see cref="AttributesOptimizer.OptimizeFromFirstYearOfPlan"/> on a background thread
    /// and exposes current vs optimal attributes with time savings.
    /// </summary>
    public sealed class PlanOptimizerViewModel : ViewModelBase
    {
        private Dictionary<EveAttribute, int> _currentAttributes = new();
        private Dictionary<EveAttribute, int> _optimalAttributes = new();
        private TimeSpan _timeSaved;
        private TimeSpan _currentDuration;
        private TimeSpan _optimalDuration;
        private bool _isCalculating;
        private bool _hasResults;

        public PlanOptimizerViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public PlanOptimizerViewModel()
        {
        }

        public Dictionary<EveAttribute, int> CurrentAttributes
        {
            get => _currentAttributes;
            set => SetProperty(ref _currentAttributes, value);
        }

        public Dictionary<EveAttribute, int> OptimalAttributes
        {
            get => _optimalAttributes;
            set => SetProperty(ref _optimalAttributes, value);
        }

        public TimeSpan TimeSaved
        {
            get => _timeSaved;
            set
            {
                if (SetProperty(ref _timeSaved, value))
                    OnPropertyChanged(nameof(TimeSavedText));
            }
        }

        public string TimeSavedText => _timeSaved > TimeSpan.Zero
            ? PlanTimeCardViewModel.FormatTrainingTime(_timeSaved)
            : string.Empty;

        public TimeSpan CurrentDuration
        {
            get => _currentDuration;
            set
            {
                if (SetProperty(ref _currentDuration, value))
                    OnPropertyChanged(nameof(CurrentDurationText));
            }
        }

        public string CurrentDurationText => _currentDuration > TimeSpan.Zero
            ? PlanTimeCardViewModel.FormatTrainingTime(_currentDuration)
            : "—";

        public TimeSpan OptimalDuration
        {
            get => _optimalDuration;
            set
            {
                if (SetProperty(ref _optimalDuration, value))
                    OnPropertyChanged(nameof(OptimalDurationText));
            }
        }

        public string OptimalDurationText => _optimalDuration > TimeSpan.Zero
            ? PlanTimeCardViewModel.FormatTrainingTime(_optimalDuration)
            : "—";

        public bool IsCalculating
        {
            get => _isCalculating;
            set => SetProperty(ref _isCalculating, value);
        }

        public bool HasResults
        {
            get => _hasResults;
            set => SetProperty(ref _hasResults, value);
        }

        /// <summary>
        /// Returns the current attribute value for the given attribute, or 0 if unavailable.
        /// </summary>
        public int GetCurrent(EveAttribute attr) =>
            _currentAttributes.TryGetValue(attr, out var v) ? v : 0;

        /// <summary>
        /// Returns the optimal attribute value for the given attribute, or 0 if unavailable.
        /// </summary>
        public int GetOptimal(EveAttribute attr) =>
            _optimalAttributes.TryGetValue(attr, out var v) ? v : 0;

        /// <summary>
        /// Runs the attribute optimization on a background thread.
        /// Safe to call from UI — posts results back via property change.
        /// </summary>
        public void RunOptimization(BasePlan? plan, Character? character)
        {
            if (plan == null || character == null)
                return;

            IsCalculating = true;
            HasResults = false;

            Task.Run(() =>
            {
                try
                {
                    RemappingResult result = AttributesOptimizer.OptimizeFromFirstYearOfPlan(plan);

                    var baseScratch = result.BaseScratchpad;
                    var bestScratch = result.BestScratchpad;

                    var current = new Dictionary<EveAttribute, int>
                    {
                        [EveAttribute.Intelligence] = (int)baseScratch.Intelligence.Base,
                        [EveAttribute.Perception] = (int)baseScratch.Perception.Base,
                        [EveAttribute.Charisma] = (int)baseScratch.Charisma.Base,
                        [EveAttribute.Willpower] = (int)baseScratch.Willpower.Base,
                        [EveAttribute.Memory] = (int)baseScratch.Memory.Base,
                    };

                    Dictionary<EveAttribute, int>? optimal = null;
                    if (bestScratch != null)
                    {
                        optimal = new Dictionary<EveAttribute, int>
                        {
                            [EveAttribute.Intelligence] = (int)bestScratch.Intelligence.Base,
                            [EveAttribute.Perception] = (int)bestScratch.Perception.Base,
                            [EveAttribute.Charisma] = (int)bestScratch.Charisma.Base,
                            [EveAttribute.Willpower] = (int)bestScratch.Willpower.Base,
                            [EveAttribute.Memory] = (int)bestScratch.Memory.Base,
                        };
                    }

                    var baseDuration = result.BaseDuration;
                    var bestDuration = result.BestDuration;
                    var saved = baseDuration - bestDuration;

                    // Post back to VM (thread-safe via property setters)
                    CurrentAttributes = current;
                    OptimalAttributes = optimal ?? current;
                    CurrentDuration = baseDuration;
                    OptimalDuration = bestDuration;
                    TimeSaved = saved > TimeSpan.Zero ? saved : TimeSpan.Zero;
                    HasResults = true;
                    IsCalculating = false;
                }
                catch
                {
                    // Optimization failed — degrade gracefully
                    IsCalculating = false;
                }
            });
        }
    }
}
