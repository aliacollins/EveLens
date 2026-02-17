using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the skill plan editor. Manages the current plan display,
    /// sort column, and sort direction.
    /// </summary>
    public sealed class PlanEditorViewModel : CharacterViewModelBase
    {
        private Plan? _plan;
        private PlanColumn _sortColumn;
        private bool _sortAscending = true;

        public PlanEditorViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            Subscribe<PlanChangedEvent>(OnPlanChanged);
        }

        public PlanEditorViewModel() : base()
        {
            Subscribe<PlanChangedEvent>(OnPlanChanged);
        }

        /// <summary>
        /// Gets or sets the currently displayed plan.
        /// </summary>
        public Plan? Plan
        {
            get => _plan;
            set => SetProperty(ref _plan, value);
        }

        /// <summary>
        /// Gets or sets the sort column for plan entries.
        /// </summary>
        public PlanColumn SortColumn
        {
            get => _sortColumn;
            set => SetProperty(ref _sortColumn, value);
        }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        public bool SortAscending
        {
            get => _sortAscending;
            set => SetProperty(ref _sortAscending, value);
        }

        /// <summary>
        /// Toggles sort on a column.
        /// </summary>
        public void ToggleSort(PlanColumn column)
        {
            if (_sortColumn == column)
            {
                SortAscending = !_sortAscending;
            }
            else
            {
                _sortAscending = true;
                SortColumn = column;
            }
        }

        private void OnPlanChanged(PlanChangedEvent e)
        {
            if (e.Plan == _plan)
            {
                OnPropertyChanged(nameof(Plan));
            }
        }
    }
}
