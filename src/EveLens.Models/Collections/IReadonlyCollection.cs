// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EveLens.Common.Collections
{
    /// <summary>
    /// Represents a read-only collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadonlyCollection<out T> : IEnumerable<T>
    {
        int Count { get; }
    }
}