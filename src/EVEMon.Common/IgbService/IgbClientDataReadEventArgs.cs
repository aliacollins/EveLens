// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;

namespace EVEMon.Common.IgbService
{
    public class IgbClientDataReadEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgbClientDataReadEventArgs"/> class.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="count">The count.</param>
        public IgbClientDataReadEventArgs(IEnumerable<byte> buffer, int count)
        {
            Buffer = buffer;
            Count = count;
        }

        /// <summary>
        /// Gets or sets the buffer.
        /// </summary>
        /// <value>The buffer.</value>
        public IEnumerable<byte> Buffer { get; }

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; }
    }
}