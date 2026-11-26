// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;

namespace EveLens.Common.ViewModels.Binding
{
    /// <summary>
    /// Forwards property changes of a source object to a callback while a view is attached.
    /// The source is kept across <see cref="Detach"/> and <see cref="Attach"/>, so a view that is
    /// removed from the visual tree and shown again with the same source keeps receiving changes.
    /// </summary>
    /// <remarks>
    /// The handler is subscribed exactly when both a source is set and the view is attached.
    /// <see cref="Attach"/> invokes the callback once when a source is present, because changes
    /// raised while detached were not delivered.
    /// </remarks>
    public sealed class AttachedSourceSubscription<T> where T : class, INotifyPropertyChanged
    {
        private readonly Action<T> _onChanged;
        private bool _subscribed;

        public AttachedSourceSubscription(Action<T> onChanged)
        {
            _onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
        }

        /// <summary>The current source, or null when none is set.</summary>
        public T? Source { get; private set; }

        /// <summary>True between <see cref="Attach"/> and <see cref="Detach"/>.</summary>
        public bool IsAttached { get; private set; }

        /// <summary>
        /// Replaces the source. The callback is not invoked; the caller displays the new source.
        /// </summary>
        public void SetSource(T? source)
        {
            if (ReferenceEquals(Source, source))
                return;

            Unsubscribe();
            Source = source;
            Subscribe();
        }

        /// <summary>
        /// Marks the view as attached, subscribes to the source and invokes the callback once
        /// when a source is present. Calling it while already attached does nothing.
        /// </summary>
        public void Attach()
        {
            if (IsAttached)
                return;

            IsAttached = true;
            Subscribe();
            if (Source != null)
                _onChanged(Source);
        }

        /// <summary>Marks the view as detached and unsubscribes. The source is kept.</summary>
        public void Detach()
        {
            IsAttached = false;
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || !IsAttached || Source == null)
                return;

            Source.PropertyChanged += OnSourcePropertyChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || Source == null)
                return;

            Source.PropertyChanged -= OnSourcePropertyChanged;
            _subscribed = false;
        }

        private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (Source != null)
                _onChanged(Source);
        }
    }
}
