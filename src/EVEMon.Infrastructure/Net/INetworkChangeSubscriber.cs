// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Net
{
    /// <summary>
    /// This interface allows implementers to register to the <see cref="NetworkMonitor"/> class to track network availability changes.
    /// </summary>
    public interface INetworkChangeSubscriber
    {
        /// <summary>
        /// Notifies the network availability changed.
        /// </summary>
        bool SetNetworkStatus { get; set; }
    }
}