// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Sales
{
    /// <summary>
    /// Thrown when something goes wrong with the mineral parser.
    /// </summary>
    [Serializable]
    public class MineralParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MineralParserException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public MineralParserException(string message)
            : base(message)
        {
        }
    }
}