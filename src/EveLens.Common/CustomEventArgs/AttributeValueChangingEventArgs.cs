// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.CustomEventArgs
{
    public sealed class AttributeValueChangingEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeValueChangingEventArgs"/> class.
        /// </summary>
        /// <param name="deltaValue">The delta value.</param>
        public AttributeValueChangingEventArgs(long deltaValue)
        {
            Value = deltaValue;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public long Value { get; }
    }
}
