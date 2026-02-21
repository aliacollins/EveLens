// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EVEMon.Common.Collections
{
    /// <summary>
    /// Represents a read-only collection
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public interface IReadonlyKeyedCollection<in TKey, out TItem> : IEnumerable<TItem>
    {
        int Count { get; }
        TItem this[TKey key] { get; }
    }
}