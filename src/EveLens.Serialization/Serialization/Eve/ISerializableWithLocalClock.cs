// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Serialization.Eve
{
    /// <summary>
    /// An interface for serializable classes which contains datetimes
    /// </summary>
    public interface ISynchronizableWithLocalClock
    {
        /// <summary>
        /// Fixup the currentTime and cachedUntil time to match the user's clock.
        /// This should ONLY be called when the xml is first recieved from CCP
        /// </summary>
        /// <param name="drift">The time span the stored times should be susbtracted with</param>
        void SynchronizeWithLocalClock(TimeSpan drift);
    }
}