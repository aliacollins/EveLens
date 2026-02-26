// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;

namespace EveLens.Common.ViewModels.Binding
{
    /// <summary>
    /// Collects <see cref="IDisposable"/> instances and disposes them all at once.
    /// Used by <see cref="ViewModelBase"/> to track event subscriptions for Law #11 compliance.
    /// Thread-safe: all operations lock on the internal list.
    /// </summary>
    internal sealed class CompositeDisposable : IDisposable
    {
        private readonly object _lock = new object();
        private List<IDisposable>? _disposables = new List<IDisposable>();

        /// <summary>
        /// Gets the number of tracked disposables.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _disposables?.Count ?? 0;
                }
            }
        }

        /// <summary>
        /// Gets whether this composite has been disposed.
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                lock (_lock)
                {
                    return _disposables == null;
                }
            }
        }

        /// <summary>
        /// Adds a disposable to the collection. If already disposed, the disposable is
        /// immediately disposed and not added.
        /// </summary>
        public void Add(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));

            bool shouldDispose = false;
            lock (_lock)
            {
                if (_disposables == null)
                    shouldDispose = true;
                else
                    _disposables.Add(disposable);
            }

            if (shouldDispose)
                disposable.Dispose();
        }

        /// <summary>
        /// Disposes all tracked disposables in reverse order and marks this instance as disposed.
        /// Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            List<IDisposable>? toDispose;
            lock (_lock)
            {
                toDispose = _disposables;
                _disposables = null;
            }

            if (toDispose == null)
                return;

            // Dispose in reverse order (LIFO) for proper cleanup ordering
            for (int i = toDispose.Count - 1; i >= 0; i--)
            {
                try
                {
                    toDispose[i].Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CompositeDisposable: error disposing item {i}: {ex}");
                }
            }
        }
    }
}
