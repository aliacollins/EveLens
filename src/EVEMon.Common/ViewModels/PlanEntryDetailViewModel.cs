using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel showing detail for a selected plan entry: skill info, attributes,
    /// training time, and prerequisites.
    /// </summary>
    public sealed class PlanEntryDetailViewModel : ViewModelBase
    {
        private PlanEntry? _selectedEntry;
        private bool _hasSelection;
        private string _skillName = string.Empty;
        private string _skillDescription = string.Empty;
        private string _primaryAttribute = string.Empty;
        private string _secondaryAttribute = string.Empty;
        private string _trainingTime = string.Empty;
        private string _skillPointsRequired = string.Empty;
        private string _spPerHour = string.Empty;
        private IReadOnlyList<PrerequisiteInfo> _prerequisites = Array.Empty<PrerequisiteInfo>();

        public PlanEntryDetailViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public PlanEntryDetailViewModel()
        {
        }

        public PlanEntry? SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (SetProperty(ref _selectedEntry, value))
                    UpdateDetail();
            }
        }

        public bool HasSelection
        {
            get => _hasSelection;
            private set => SetProperty(ref _hasSelection, value);
        }

        public string SkillName
        {
            get => _skillName;
            private set => SetProperty(ref _skillName, value);
        }

        public string SkillDescription
        {
            get => _skillDescription;
            private set => SetProperty(ref _skillDescription, value);
        }

        public string PrimaryAttribute
        {
            get => _primaryAttribute;
            private set => SetProperty(ref _primaryAttribute, value);
        }

        public string SecondaryAttribute
        {
            get => _secondaryAttribute;
            private set => SetProperty(ref _secondaryAttribute, value);
        }

        public string TrainingTime
        {
            get => _trainingTime;
            private set => SetProperty(ref _trainingTime, value);
        }

        public string SkillPointsRequired
        {
            get => _skillPointsRequired;
            private set => SetProperty(ref _skillPointsRequired, value);
        }

        public string SpPerHour
        {
            get => _spPerHour;
            private set => SetProperty(ref _spPerHour, value);
        }

        public IReadOnlyList<PrerequisiteInfo> Prerequisites
        {
            get => _prerequisites;
            private set => SetProperty(ref _prerequisites, value);
        }

        private void UpdateDetail()
        {
            if (_selectedEntry == null)
            {
                HasSelection = false;
                SkillName = string.Empty;
                SkillDescription = string.Empty;
                PrimaryAttribute = string.Empty;
                SecondaryAttribute = string.Empty;
                TrainingTime = string.Empty;
                SkillPointsRequired = string.Empty;
                SpPerHour = string.Empty;
                Prerequisites = Array.Empty<PrerequisiteInfo>();
                return;
            }

            HasSelection = true;
            SkillName = $"{_selectedEntry.Skill.Name} {Skill.GetRomanFromInt(_selectedEntry.Level)}";
            SkillDescription = _selectedEntry.Skill.Description;
            PrimaryAttribute = _selectedEntry.Skill.PrimaryAttribute.ToString();
            SecondaryAttribute = _selectedEntry.Skill.SecondaryAttribute.ToString();
            TrainingTime = PlanTimeCardViewModel.FormatTrainingTime(_selectedEntry.TrainingTime);
            SkillPointsRequired = _selectedEntry.SkillPointsRequired.ToString("N0") + " SP";
            SpPerHour = _selectedEntry.SpPerHour.ToString("N0") + " SP/hr";

            var prereqs = _selectedEntry.Skill.Prerequisites
                .Select(p =>
                {
                    var charSkill = _selectedEntry.CharacterSkill;
                    bool isMet = false;
                    if (_selectedEntry.Character is Character character)
                    {
                        var cSkill = p.Skill.ToCharacter(character);
                        isMet = cSkill != null && cSkill.Level >= p.Level;
                    }

                    return new PrerequisiteInfo(
                        $"{p.Skill.Name} {Skill.GetRomanFromInt(p.Level)}",
                        p.Level,
                        isMet);
                })
                .ToList();

            Prerequisites = prereqs;
        }
    }

    /// <summary>
    /// Immutable info about a single prerequisite skill for display.
    /// </summary>
    public sealed class PrerequisiteInfo
    {
        public string Name { get; }
        public long Level { get; }
        public bool IsMet { get; }

        public PrerequisiteInfo(string name, long level, bool isMet)
        {
            Name = name;
            Level = level;
            IsMet = isMet;
        }
    }
}
