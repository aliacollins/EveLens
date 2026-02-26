// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Extensions;

namespace EveLens.Common.Factories
{
    /// <summary>
    /// Implements a disposable pattern which invokes a call back once it is disposed. Use it in a <c>using</c> block.
    /// </summary>
    public struct DisposableWithCallback : IDisposable
    {
        private readonly Action m_action;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="action">The callback used when this structure will be disposed</param>
        public DisposableWithCallback(Action action)
        {
            m_action = action;
        }

        /// <summary>
        /// Dispose the structure and calls back the specified action
        /// </summary>
        public void Dispose()
        {
            m_action();
        }

        /// <summary>
        /// Performs an action and send back a Disposable which will perform another action once it is disposed. Typically used to make temporary changes through
        /// the "using" pattern.
        /// </summary>
        /// <param name="push">The action to perform right now</param>
        /// <param name="pop">The action to perform once the returned object will be disposed</param>
        /// <returns>An object implementing IDisposable</returns>
        /// <exception cref="System.ArgumentNullException">push</exception>
        public static IDisposable Begin(Action push, Action pop)
        {
            push.ThrowIfNull(nameof(push));

            push();
            return new DisposableWithCallback(pop);
        }
    }
}