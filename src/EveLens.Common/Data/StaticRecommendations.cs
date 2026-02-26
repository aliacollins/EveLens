// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Collections;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a list of recommendations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class StaticRecommendations<T> : ReadonlyCollection<T>
    {
        /// <summary>
        /// Default constructor, only used during datafiles initialization
        /// </summary>
        internal StaticRecommendations()
        {
        }

        /// <summary>
        /// Adds the given item to the recommendations list.
        /// </summary>
        /// <param name="item"></param>
        internal void Add(T item)
        {
            Items.Add(item);
        }
    }
}