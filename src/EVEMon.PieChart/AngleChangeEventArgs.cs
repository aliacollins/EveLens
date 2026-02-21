// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.PieChart
{
    public class AngleChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AngleChangeEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public AngleChangeEventArgs(float oldValue, float newValue)
        {
            OldAngle = oldValue;
            NewAngle = newValue;
        }

        /// <summary>
        /// Gets the new angle.
        /// </summary>
        /// <value>
        /// The new angle.
        /// </value>
        public float NewAngle { get; private set; }

        /// <summary>
        /// Gets the old angle.
        /// </summary>
        /// <value>
        /// The old angle.
        /// </value>
        public float OldAngle { get; private set; }
    }
}