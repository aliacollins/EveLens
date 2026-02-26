// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Default fallback <see cref="IDispatcher"/> implementation that executes actions
    /// inline on the calling thread. In production, the Avalonia UI layer replaces this
    /// with <c>AvaloniaDispatcher</c> via <c>AppServices.SetDispatcher()</c>.
    /// </summary>
    internal sealed class DispatcherService : IDispatcher
    {
        /// <inheritdoc />
        public void Invoke(Action action)
        {
            action();
        }

        /// <inheritdoc />
        public void Post(Action action)
        {
            action();
        }

        /// <inheritdoc />
        public void Schedule(TimeSpan delay, Action action)
        {
            Task.Delay(delay).ContinueWith(_ => action(), TaskScheduler.Default);
        }
    }
}
