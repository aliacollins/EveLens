// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;

namespace EVEMon.Common.CustomEventArgs
{
    /// <summary>
    /// Represents an argument for server changes
    /// </summary>
    public sealed class EveServerEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="previousStatus"></param>
        /// <param name="status"></param>
        public EveServerEventArgs(EveServer server, ServerStatus previousStatus, ServerStatus status)
        {
            Server = server;
            Status = status;
            PreviousStatus = previousStatus;
        }

        /// <summary>
        /// Gets the updated server
        /// </summary>
        public EveServer Server { get; }

        /// <summary>
        /// Gets the current status
        /// </summary>
        public ServerStatus Status { get; }

        /// <summary>
        /// Gets the previous status
        /// </summary>
        public ServerStatus PreviousStatus { get; }
    }
}