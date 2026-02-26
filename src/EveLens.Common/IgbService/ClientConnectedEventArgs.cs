// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Net.Sockets;

namespace EveLens.Common.IgbService
{
    /// <summary>
    /// Event arguments triggered on client connect.
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectedEventArgs"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public ClientConnectedEventArgs(TcpClient client)
        {
            TcpClient = client;
        }

        /// <summary>
        /// Gets or sets the TCP client.
        /// </summary>
        /// <value>The TCP client.</value>
        public TcpClient TcpClient { get; }
    }
}