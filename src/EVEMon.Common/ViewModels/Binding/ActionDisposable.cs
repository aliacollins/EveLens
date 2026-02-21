// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading;

namespace EVEMon.Common.ViewModels.Binding
{
    /// <summary>
    /// A simple <see cref="IDisposable"/> that invokes an <see cref="Action"/> exactly once on disposal.
    /// Thread-safe: the action is invoked at most once even if <see cref="Dispose"/> is called concurrently.
    /// </summary>
    internal sealed class ActionDisposable : IDisposable
    {
        private Action? _action;

        public ActionDisposable(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _action, null)?.Invoke();
        }
    }
}
