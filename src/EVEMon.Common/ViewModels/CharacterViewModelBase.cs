using System;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// Base ViewModel for character-scoped views. Provides a <see cref="Character"/> property
    /// with change notification and character-filtered event subscriptions.
    /// </summary>
    public abstract class CharacterViewModelBase : ViewModelBase
    {
        private Character? _character;

        /// <summary>
        /// Creates a new character ViewModel with explicit dependencies (testing).
        /// </summary>
        protected CharacterViewModelBase(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        /// <summary>
        /// Creates a new character ViewModel using AppServices defaults (production).
        /// </summary>
        protected CharacterViewModelBase()
        {
        }

        /// <summary>
        /// Gets or sets the character this ViewModel operates on.
        /// Setting this property calls <see cref="OnCharacterChanged"/> for subclass override.
        /// </summary>
        public Character? Character
        {
            get => _character;
            set
            {
                if (SetProperty(ref _character, value))
                {
                    OnCharacterChanged();
                }
            }
        }

        /// <summary>
        /// Forces <see cref="OnCharacterChanged"/> even when the Character reference hasn't changed.
        /// Used by Avalonia views when ESI data arrives for the same character object.
        /// </summary>
        public void ForceRefresh() => OnCharacterChanged();

        /// <summary>
        /// Called when <see cref="Character"/> changes. Override to refresh data, re-subscribe
        /// to character-specific events, etc.
        /// </summary>
        protected virtual void OnCharacterChanged()
        {
        }

        /// <summary>
        /// Subscribes to a character-scoped event, filtering to only the current <see cref="Character"/>.
        /// The handler is only invoked when the event's character matches the current character.
        /// Auto-tracked for disposal.
        /// </summary>
        /// <typeparam name="TEvent">The event type (must derive from <see cref="CharacterEventBase"/>).</typeparam>
        /// <param name="handler">The handler to invoke for matching events.</param>
        protected void SubscribeForCharacter<TEvent>(Action<TEvent> handler)
            where TEvent : CharacterEventBase
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Subscribe<TEvent>(e =>
            {
                if (_character != null && e.Character == _character)
                    handler(e);
            });
        }
    }
}
