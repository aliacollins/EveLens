// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// Base ViewModel for dialog forms with dirty tracking and apply/cancel semantics.
    /// </summary>
    public abstract class FormViewModel : ViewModelBase
    {
        private bool _isDirty;

        /// <summary>
        /// Creates a new form ViewModel with explicit dependencies (testing).
        /// </summary>
        protected FormViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        /// <summary>
        /// Creates a new form ViewModel using AppServices defaults (production).
        /// </summary>
        protected FormViewModel()
        {
        }

        /// <summary>
        /// Gets or sets whether the form has unsaved changes.
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            protected set => SetProperty(ref _isDirty, value);
        }

        /// <summary>
        /// Marks the form as dirty. Call this when any editable property changes.
        /// </summary>
        protected void MarkDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Applies changes and resets the dirty flag.
        /// Subclasses implement <see cref="OnApply"/> for the actual save logic.
        /// </summary>
        public void Apply()
        {
            OnApply();
            IsDirty = false;
        }

        /// <summary>
        /// Cancels changes and resets the dirty flag.
        /// Subclasses implement <see cref="OnCancel"/> for the actual revert logic.
        /// </summary>
        public void Cancel()
        {
            OnCancel();
            IsDirty = false;
        }

        /// <summary>
        /// Override to implement the save/apply logic.
        /// </summary>
        protected abstract void OnApply();

        /// <summary>
        /// Override to implement the cancel/revert logic.
        /// </summary>
        protected abstract void OnCancel();
    }
}
