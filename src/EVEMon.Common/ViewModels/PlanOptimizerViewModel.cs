using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVEMon.Common.Constants;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;
using EVEMon.SkillPlanner;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// Per-skill time impact from attribute optimization.
    /// </summary>
    public sealed class SkillTimeImpact
    {
        public string SkillName { get; init; } = "";
        public int Level { get; init; }
        public TimeSpan CurrentTime { get; init; }
        public TimeSpan OptimalTime { get; init; }
        public TimeSpan TimeSaved { get; init; }
        public EveAttribute PrimaryAttribute { get; init; }
        public EveAttribute SecondaryAttribute { get; init; }
    }

    /// <summary>
    /// Per-attribute contribution to time savings from optimization.
    /// </summary>
    public sealed class AttributeImpact
    {
        public EveAttribute Attribute { get; init; }
        public int CurrentBase { get; init; }
        public int OptimalBase { get; init; }
        public int Delta { get; init; }
        public int ImplantBonus { get; init; }
        public int AffectedSkillCount { get; init; }
        public TimeSpan TotalTimeSaved { get; init; }
    }

    /// <summary>
    /// ViewModel for the attribute optimizer panel.
    /// Supports 3 optimization strategies, manual +/− attribute adjustment with unassigned pool,
    /// multi-result paging for remap-point mode, and live training time recalculation.
    /// </summary>
    public sealed class PlanOptimizerViewModel : ViewModelBase
    {
        private static readonly EveAttribute[] AllAttributes =
        {
            EveAttribute.Intelligence, EveAttribute.Perception,
            EveAttribute.Charisma, EveAttribute.Willpower, EveAttribute.Memory
        };

        private Dictionary<EveAttribute, int> _currentAttributes = new();
        private Dictionary<EveAttribute, int> _optimalAttributes = new();
        private TimeSpan _timeSaved;
        private TimeSpan _currentDuration;
        private TimeSpan _optimalDuration;
        private bool _isCalculating;
        private bool _hasResults;
        private List<SkillTimeImpact> _skillImpacts = new();
        private List<AttributeImpact> _attributeImpacts = new();
        private Dictionary<EveAttribute, int> _implantBonuses = new();
        private RemappingResult? _lastResult;
        private string _errorMessage = "";

        // Interactive optimizer state
        private AttributeOptimizationStrategy _strategy = AttributeOptimizationStrategy.OneYearPlan;
        private Dictionary<EveAttribute, int> _manualRemappable = new();
        private bool _isManuallyEdited;
        private TimeSpan _manualDuration;
        private int _unassignedPoints;
        private List<RemappingResult> _allResults = new();
        private int _selectedResultIndex;

        public PlanOptimizerViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public PlanOptimizerViewModel()
        {
        }

        #region Existing Properties

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
            : "\u2014";

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
            : "\u2014";

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

        public List<SkillTimeImpact> SkillImpacts
        {
            get => _skillImpacts;
            set => SetProperty(ref _skillImpacts, value);
        }

        public List<AttributeImpact> AttributeImpacts
        {
            get => _attributeImpacts;
            set => SetProperty(ref _attributeImpacts, value);
        }

        public Dictionary<EveAttribute, int> ImplantBonuses
        {
            get => _implantBonuses;
            set => SetProperty(ref _implantBonuses, value);
        }

        public RemappingResult? LastResult
        {
            get => _lastResult;
            set => SetProperty(ref _lastResult, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        #endregion

        #region Interactive Optimizer Properties

        public AttributeOptimizationStrategy Strategy
        {
            get => _strategy;
            set => SetProperty(ref _strategy, value);
        }

        public Dictionary<EveAttribute, int> ManualRemappable
        {
            get => _manualRemappable;
            set => SetProperty(ref _manualRemappable, value);
        }

        public bool IsManuallyEdited
        {
            get => _isManuallyEdited;
            set => SetProperty(ref _isManuallyEdited, value);
        }

        public TimeSpan ManualDuration
        {
            get => _manualDuration;
            set
            {
                if (SetProperty(ref _manualDuration, value))
                    OnPropertyChanged(nameof(ManualDurationText));
            }
        }

        public string ManualDurationText => _manualDuration > TimeSpan.Zero
            ? PlanTimeCardViewModel.FormatTrainingTime(_manualDuration)
            : "\u2014";

        public int UnassignedPoints
        {
            get => _unassignedPoints;
            set => SetProperty(ref _unassignedPoints, value);
        }

        public List<RemappingResult> AllResults
        {
            get => _allResults;
            set
            {
                if (SetProperty(ref _allResults, value))
                    OnPropertyChanged(nameof(HasMultipleResults));
            }
        }

        public int SelectedResultIndex
        {
            get => _selectedResultIndex;
            set
            {
                if (SetProperty(ref _selectedResultIndex, value))
                    OnPropertyChanged(nameof(SelectedResult));
            }
        }

        public RemappingResult? SelectedResult =>
            _selectedResultIndex >= 0 && _selectedResultIndex < _allResults.Count
                ? _allResults[_selectedResultIndex]
                : null;

        public bool HasMultipleResults => _allResults.Count > 1;

        #endregion

        #region Attribute Accessors

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
        /// Gets the remappable points (0-10) for the given attribute.
        /// Remappable = allocated points above the base of 17.
        /// </summary>
        public int GetRemappable(EveAttribute attr) =>
            _manualRemappable.TryGetValue(attr, out var v) ? v : 0;

        /// <summary>
        /// Whether the attribute can be incremented (+1).
        /// </summary>
        public bool CanIncrement(EveAttribute attr) =>
            _unassignedPoints > 0 && GetRemappable(attr) < EveConstants.MaxRemappablePointsPerAttribute;

        /// <summary>
        /// Whether the attribute can be decremented (-1).
        /// </summary>
        public bool CanDecrement(EveAttribute attr) =>
            GetRemappable(attr) > 0;

        /// <summary>
        /// Returns the implant bonus for the given attribute.
        /// </summary>
        public int GetImplantBonus(EveAttribute attr) =>
            _implantBonuses.TryGetValue(attr, out var v) ? v : 0;

        #endregion

        #region Manual Adjustment

        /// <summary>
        /// Adjusts the manual attribute value by delta (+1 or -1).
        /// Validates pool constraints and recalculates training time.
        /// </summary>
        public void AdjustAttribute(EveAttribute attr, int delta)
        {
            int current = GetRemappable(attr);
            int newVal = current + delta;

            if (newVal < 0 || newVal > EveConstants.MaxRemappablePointsPerAttribute)
                return;

            int newUnassigned = _unassignedPoints - delta;
            if (newUnassigned < 0 || newUnassigned > EveConstants.SpareAttributePointsOnRemap)
                return;

            _manualRemappable[attr] = newVal;
            UnassignedPoints = newUnassigned;
            IsManuallyEdited = true;

            RecalculateManual();
            OnPropertyChanged(nameof(ManualRemappable));
        }

        /// <summary>
        /// Recalculates training time using current manual attribute values.
        /// </summary>
        private void RecalculateManual()
        {
            var activeResult = SelectedResult ?? _lastResult;
            if (activeResult?.BaseScratchpad == null)
                return;

            var manualScratchpad = activeResult.BaseScratchpad.Clone();
            foreach (var attr in AllAttributes)
            {
                int remappable = GetRemappable(attr);
                manualScratchpad[attr].Base = remappable + EveConstants.CharacterBaseAttributePoints;
            }

            var manualResult = new RemappingResult(activeResult, manualScratchpad);
            manualResult.Update();

            ManualDuration = manualResult.BestDuration;

            // Update optimal to reflect manual values
            var newOptimal = new Dictionary<EveAttribute, int>();
            foreach (var attr in AllAttributes)
                newOptimal[attr] = GetRemappable(attr) + EveConstants.CharacterBaseAttributePoints;
            OptimalAttributes = newOptimal;

            // Recalculate savings
            TimeSaved = _currentDuration > ManualDuration && ManualDuration > TimeSpan.Zero
                ? _currentDuration - ManualDuration
                : TimeSpan.Zero;
            OptimalDuration = ManualDuration;
        }

        /// <summary>
        /// Resets manual adjustments to the optimizer's computed optimal values.
        /// </summary>
        public void ResetToOptimal()
        {
            if (_lastResult?.BestScratchpad == null) return;

            InitializeManualFromScratchpad(_lastResult.BestScratchpad);
            IsManuallyEdited = false;
            OptimalDuration = _lastResult.BestDuration;
            TimeSaved = _currentDuration > _lastResult.BestDuration
                ? _currentDuration - _lastResult.BestDuration
                : TimeSpan.Zero;

            var optimal = new Dictionary<EveAttribute, int>();
            foreach (var attr in AllAttributes)
                optimal[attr] = (int)_lastResult.BestScratchpad[attr].Base;
            OptimalAttributes = optimal;

            OnPropertyChanged(nameof(ManualRemappable));
            OnPropertyChanged(nameof(UnassignedPoints));
        }

        /// <summary>
        /// Resets to the character's current attributes.
        /// </summary>
        public void ResetToCurrent()
        {
            if (_lastResult?.BaseScratchpad == null) return;

            InitializeManualFromScratchpad(_lastResult.BaseScratchpad);
            IsManuallyEdited = true;
            RecalculateManual();

            OnPropertyChanged(nameof(ManualRemappable));
            OnPropertyChanged(nameof(UnassignedPoints));
        }

        /// <summary>
        /// Clears all results to prepare for a new optimization run.
        /// </summary>
        public void ClearResults()
        {
            HasResults = false;
            ErrorMessage = "";
            AllResults = new List<RemappingResult>();
            SelectedResultIndex = 0;
            IsManuallyEdited = false;
        }

        private void InitializeManualFromScratchpad(CharacterScratchpad scratchpad)
        {
            int totalRemappable = 0;
            foreach (var attr in AllAttributes)
            {
                int remappable = (int)scratchpad[attr].Base - EveConstants.CharacterBaseAttributePoints;
                remappable = Math.Max(0, Math.Min(EveConstants.MaxRemappablePointsPerAttribute, remappable));
                _manualRemappable[attr] = remappable;
                totalRemappable += remappable;
            }
            UnassignedPoints = EveConstants.SpareAttributePointsOnRemap - totalRemappable;
        }

        #endregion

        #region Optimization

        /// <summary>
        /// Runs the attribute optimization using the current strategy.
        /// Safe to call from UI — posts results back via property change.
        /// </summary>
        public void RunOptimization(BasePlan? plan, Character? character)
        {
            RunOptimization(plan, character, _strategy);
        }

        /// <summary>
        /// Runs the attribute optimization with a specific strategy.
        /// Safe to call from UI — posts results back via property change.
        /// </summary>
        public void RunOptimization(BasePlan? plan, Character? character, AttributeOptimizationStrategy strategy)
        {
            if (plan == null || character == null)
                return;

            Strategy = strategy;
            IsCalculating = true;
            HasResults = false;
            IsManuallyEdited = false;

            Task.Run(() =>
            {
                try
                {
                    RemappingResult? result = null;
                    List<RemappingResult>? multiResults = null;

                    switch (strategy)
                    {
                        case AttributeOptimizationStrategy.OneYearPlan:
                            result = AttributesOptimizer.OptimizeFromFirstYearOfPlan(plan);
                            break;

                        case AttributeOptimizationStrategy.RemappingPoints:
                            var results = AttributesOptimizer.OptimizeFromPlanAndRemappingPoints(plan);
                            multiResults = results.ToList();
                            if (multiResults.Count > 0)
                                result = multiResults[0];
                            break;

                        case AttributeOptimizationStrategy.Character:
                            result = AttributesOptimizer.OptimizeFromCharacter(character, plan);
                            break;

                        default:
                            result = AttributesOptimizer.OptimizeFromFirstYearOfPlan(plan);
                            break;
                    }

                    if (result == null)
                    {
                        ErrorMessage = strategy == AttributeOptimizationStrategy.RemappingPoints
                            ? "No remap points found in plan. Add remap points first."
                            : "Optimization produced no result.";
                        IsCalculating = false;
                        return;
                    }

                    AllResults = multiResults ?? new List<RemappingResult> { result };
                    SelectedResultIndex = 0;

                    PopulateFromResult(result);

                    LastResult = result;
                    HasResults = true;
                    IsCalculating = false;
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.ToString();
                    IsCalculating = false;
                }
            });
        }

        private void PopulateFromResult(RemappingResult result)
        {
            var baseScratch = result.BaseScratchpad;
            var bestScratch = result.BestScratchpad;

            var current = new Dictionary<EveAttribute, int>();
            Dictionary<EveAttribute, int>? optimal = null;

            foreach (var attr in AllAttributes)
                current[attr] = (int)baseScratch[attr].Base;

            if (bestScratch != null)
            {
                optimal = new Dictionary<EveAttribute, int>();
                foreach (var attr in AllAttributes)
                    optimal[attr] = (int)bestScratch[attr].Base;
            }

            var baseDuration = result.BaseDuration;
            var bestDuration = result.BestDuration;
            var saved = baseDuration - bestDuration;

            // Per-skill time impact
            var skillImpacts = new List<SkillTimeImpact>();
            foreach (var skillLevel in result.Skills)
            {
                var currentTime = baseScratch.GetTrainingTime(skillLevel);
                var optimalTime = bestScratch != null
                    ? bestScratch.GetTrainingTime(skillLevel)
                    : currentTime;
                skillImpacts.Add(new SkillTimeImpact
                {
                    SkillName = skillLevel.Skill.Name,
                    Level = (int)skillLevel.Level,
                    CurrentTime = currentTime,
                    OptimalTime = optimalTime,
                    TimeSaved = currentTime - optimalTime,
                    PrimaryAttribute = skillLevel.Skill.PrimaryAttribute,
                    SecondaryAttribute = skillLevel.Skill.SecondaryAttribute,
                });
            }
            skillImpacts.Sort((a, b) =>
                Math.Abs(b.TimeSaved.Ticks).CompareTo(Math.Abs(a.TimeSaved.Ticks)));

            // Per-attribute impact
            var attrImpacts = new List<AttributeImpact>();
            foreach (var attr in AllAttributes)
            {
                int curBase = current[attr];
                int optBase = (optimal ?? current)[attr];
                var affected = skillImpacts
                    .Where(s => s.PrimaryAttribute == attr || s.SecondaryAttribute == attr)
                    .ToList();
                var totalSaved = TimeSpan.FromTicks(affected.Sum(s => s.TimeSaved.Ticks));
                attrImpacts.Add(new AttributeImpact
                {
                    Attribute = attr,
                    CurrentBase = curBase,
                    OptimalBase = optBase,
                    Delta = optBase - curBase,
                    ImplantBonus = (int)baseScratch[attr].ImplantBonus,
                    AffectedSkillCount = affected.Count,
                    TotalTimeSaved = totalSaved,
                });
            }

            // Implant bonuses
            var implants = new Dictionary<EveAttribute, int>();
            foreach (var attr in AllAttributes)
                implants[attr] = (int)baseScratch[attr].ImplantBonus;

            // Post back to VM
            CurrentAttributes = current;
            OptimalAttributes = optimal ?? current;
            CurrentDuration = baseDuration;
            OptimalDuration = bestDuration;
            TimeSaved = saved > TimeSpan.Zero ? saved : TimeSpan.Zero;
            SkillImpacts = skillImpacts;
            AttributeImpacts = attrImpacts;
            ImplantBonuses = implants;

            // Initialize manual remappable values from optimal
            InitializeManualFromScratchpad(bestScratch ?? baseScratch);
        }

        #endregion
    }
}
