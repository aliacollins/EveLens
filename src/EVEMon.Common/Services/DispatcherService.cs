// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Strangler Fig wrapper for the static <see cref="Threading.Dispatcher"/> class.
    /// Implements <see cref="IDispatcher"/> by delegating to the existing static methods.
    /// </summary>
    internal sealed class DispatcherService : IDispatcher
    {
        /// <inheritdoc />
        public void Invoke(Action action)
        {
            Threading.Dispatcher.Invoke(action);
        }

        /// <inheritdoc />
        public void Post(Action action)
        {
            Threading.Dispatcher.Post(action);
        }

        /// <inheritdoc />
        public void Schedule(TimeSpan delay, Action action)
        {
            Threading.Dispatcher.Schedule(delay, action);
        }
    }
}
