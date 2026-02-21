// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Eve;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVEMon.Common.Service
{
    /// <summary>
    /// Represents a pending request for structure information.
    /// Tracks which characters have been tried and manages state across multiple callers.
    /// Uses TaskCompletionSource to allow multiple callers to await the same request.
    /// </summary>
    internal sealed class PendingStructureRequest
    {
        /// <summary>
        /// The structure ID being requested.
        /// </summary>
        public long StructureID { get; }

        /// <summary>
        /// Current state of the request.
        /// </summary>
        public StructureRequestState State { get; private set; }

        /// <summary>
        /// The result if completed successfully, null otherwise.
        /// </summary>
        public SerializableOutpost Result { get; private set; }

        /// <summary>
        /// Characters that have already been tried (by CharacterID to avoid ESIKey comparison issues).
        /// </summary>
        private readonly HashSet<long> _triedCharacters;

        /// <summary>
        /// TaskCompletionSource for async waiting - allows multiple callers to await same request.
        /// </summary>
        private readonly TaskCompletionSource<SerializableOutpost> _completionSource;

        /// <summary>
        /// Lock for thread safety.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Creates a new pending request for the specified structure ID.
        /// </summary>
        /// <param name="structureId">The structure ID to request.</param>
        public PendingStructureRequest(long structureId)
        {
            StructureID = structureId;
            State = StructureRequestState.Pending;
            _triedCharacters = new HashSet<long>();
            _completionSource = new TaskCompletionSource<SerializableOutpost>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// Gets a task that completes when this request is resolved.
        /// Multiple callers can safely await this.
        /// </summary>
        public Task<SerializableOutpost> WaitAsync() => _completionSource.Task;

        /// <summary>
        /// Marks a character as tried for this structure.
        /// Returns false if already tried.
        /// </summary>
        /// <param name="characterId">The character ID to mark as tried.</param>
        /// <returns>True if marked, false if already tried.</returns>
        public bool TryMarkCharacterAttempted(long characterId)
        {
            lock (_lock)
            {
                if (_triedCharacters.Contains(characterId))
                    return false;
                _triedCharacters.Add(characterId);
                return true;
            }
        }

        /// <summary>
        /// Gets characters that haven't been tried yet from the provided candidates.
        /// </summary>
        /// <param name="candidates">The candidate characters to filter.</param>
        /// <returns>Characters that haven't been tried yet.</returns>
        public IEnumerable<CCPCharacter> GetUntriedCharacters(IEnumerable<CCPCharacter> candidates)
        {
            lock (_lock)
            {
                return candidates.Where(c => !_triedCharacters.Contains(c.CharacterID)).ToList();
            }
        }

        /// <summary>
        /// Gets the count of characters that have been tried.
        /// </summary>
        public int TriedCharacterCount
        {
            get
            {
                lock (_lock)
                {
                    return _triedCharacters.Count;
                }
            }
        }

        /// <summary>
        /// Marks the request as in-progress.
        /// </summary>
        public void SetInProgress()
        {
            lock (_lock)
            {
                if (State == StructureRequestState.Pending)
                    State = StructureRequestState.InProgress;
            }
        }

        /// <summary>
        /// Completes the request successfully with the result.
        /// </summary>
        /// <param name="result">The structure information.</param>
        public void Complete(SerializableOutpost result)
        {
            lock (_lock)
            {
                Result = result;
                State = StructureRequestState.Completed;
            }
            _completionSource.TrySetResult(result);
        }

        /// <summary>
        /// Marks the structure as inaccessible (all characters tried, none succeeded).
        /// </summary>
        public void SetInaccessible()
        {
            lock (_lock)
            {
                State = StructureRequestState.Inaccessible;
            }
            _completionSource.TrySetResult(null);
        }

        /// <summary>
        /// Marks the structure as destroyed (404 from ESI).
        /// </summary>
        public void SetDestroyed()
        {
            lock (_lock)
            {
                State = StructureRequestState.Destroyed;
            }
            _completionSource.TrySetResult(null);
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"Structure {StructureID:D}: {State} (tried {TriedCharacterCount} chars)";
        }
    }
}
