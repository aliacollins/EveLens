using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the plan goal card showing completion progress.
    /// Updated by <see cref="PlanDashboardViewModel"/>.
    /// </summary>
    public sealed class PlanGoalCardViewModel : ViewModelBase
    {
        private string _planName = string.Empty;
        private int _skillsTrained;
        private int _skillsMissing;
        private int _totalSkills;

        public PlanGoalCardViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public PlanGoalCardViewModel()
        {
        }

        public string PlanName
        {
            get => _planName;
            set => SetProperty(ref _planName, value);
        }

        public int SkillsTrained
        {
            get => _skillsTrained;
            set
            {
                if (SetProperty(ref _skillsTrained, value))
                {
                    OnPropertyChanged(nameof(ProgressPercent));
                    OnPropertyChanged(nameof(ProgressFraction));
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public int SkillsMissing
        {
            get => _skillsMissing;
            set => SetProperty(ref _skillsMissing, value);
        }

        public int TotalSkills
        {
            get => _totalSkills;
            set
            {
                if (SetProperty(ref _totalSkills, value))
                {
                    OnPropertyChanged(nameof(ProgressPercent));
                    OnPropertyChanged(nameof(ProgressFraction));
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        /// <summary>
        /// Gets the completion percentage (0-100).
        /// </summary>
        public double ProgressPercent => _totalSkills > 0
            ? (double)_skillsTrained / _totalSkills * 100.0
            : 0.0;

        /// <summary>
        /// Gets the completion fraction (0-1) for progress bar binding.
        /// </summary>
        public double ProgressFraction => _totalSkills > 0
            ? (double)_skillsTrained / _totalSkills
            : 0.0;

        /// <summary>
        /// Gets a human-readable progress description.
        /// </summary>
        public string ProgressText => $"{_skillsTrained} of {_totalSkills} skills trained";
    }
}
