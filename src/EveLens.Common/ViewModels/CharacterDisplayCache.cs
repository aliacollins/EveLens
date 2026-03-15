// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// LRU cache that tracks which characters have active display data.
    /// When capacity is exceeded, the least recently used character's display data
    /// is eligible for eviction. Not thread-safe — assumes UI thread affinity.
    /// </summary>
    public sealed class CharacterDisplayCache
    {
        private readonly int _capacity;
        private readonly LinkedList<long> _accessOrder = new(); // most recent at front
        private readonly Dictionary<long, LinkedListNode<long>> _nodes = new();

        public CharacterDisplayCache(int capacity = 5)
        {
            _capacity = capacity > 0 ? capacity : 1;
        }

        /// <summary>
        /// Record access to a character. Promotes it to most-recently-used.
        /// Returns the evicted character ID if capacity was exceeded, or null.
        /// </summary>
        public long? Touch(long characterId)
        {
            if (_nodes.TryGetValue(characterId, out var existing))
            {
                // Already cached — promote to front
                _accessOrder.Remove(existing);
                _accessOrder.AddFirst(existing);
                return null;
            }

            // New entry
            var node = _accessOrder.AddFirst(characterId);
            _nodes[characterId] = node;

            // Evict LRU if over capacity
            if (_nodes.Count > _capacity)
            {
                var lru = _accessOrder.Last!;
                long evictedId = lru.Value;
                _accessOrder.RemoveLast();
                _nodes.Remove(evictedId);
                return evictedId;
            }

            return null;
        }

        /// <summary>Check if a character is in the cache.</summary>
        public bool Contains(long characterId) => _nodes.ContainsKey(characterId);

        /// <summary>Remove a specific character (e.g., on character delete).</summary>
        public void Remove(long characterId)
        {
            if (_nodes.TryGetValue(characterId, out var node))
            {
                _accessOrder.Remove(node);
                _nodes.Remove(characterId);
            }
        }

        /// <summary>Current cached character IDs in access order (most recent first).</summary>
        public IReadOnlyList<long> CachedCharacters
        {
            get
            {
                var list = new List<long>(_nodes.Count);
                foreach (var id in _accessOrder)
                    list.Add(id);
                return list;
            }
        }

        public int Capacity => _capacity;

        public int Count => _nodes.Count;
    }
}
