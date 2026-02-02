using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EVEMon.Common.Models;

namespace EVEMon.Common.Helpers
{
    /// <summary>
    /// Batches character update events to prevent UI cascade when many characters update rapidly.
    /// Instead of firing 100 individual CharacterUpdated events, collects updates within a
    /// coalesce window and fires a single batched event.
    /// </summary>
    public sealed class UpdateBatcher : IDisposable
    {
        #region Fields

        /// <summary>
        /// Default coalesce window in milliseconds.
        /// Updates within this window are batched together.
        /// </summary>
        private const int DefaultCoalesceMs = 100;

        private readonly HashSet<Character> _pendingCharacterUpdates = new HashSet<Character>();
        private readonly HashSet<Character> _pendingSkillQueueUpdates = new HashSet<Character>();
        private readonly object _lock = new object();
        private Timer _flushTimer;
        private bool _disposed;
        private int _coalesceMs;

        #endregion


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateBatcher"/> class.
        /// </summary>
        /// <param name="coalesceMs">The coalesce window in milliseconds.</param>
        public UpdateBatcher(int coalesceMs = DefaultCoalesceMs)
        {
            _coalesceMs = coalesceMs;
        }

        #endregion


        #region Events

        /// <summary>
        /// Fired when batched character updates are ready to be processed.
        /// Contains all characters that were updated within the coalesce window.
        /// </summary>
        public event EventHandler<CharacterBatchEventArgs> CharactersBatchUpdated;

        /// <summary>
        /// Fired when batched skill queue updates are ready to be processed.
        /// </summary>
        public event EventHandler<CharacterBatchEventArgs> SkillQueuesBatchUpdated;

        #endregion


        #region Public Methods

        /// <summary>
        /// Queues a character update to be batched.
        /// </summary>
        /// <param name="character">The character that was updated.</param>
        public void QueueCharacterUpdate(Character character)
        {
            if (_disposed || character == null)
                return;

            lock (_lock)
            {
                _pendingCharacterUpdates.Add(character);
                EnsureTimerStarted();
            }
        }

        /// <summary>
        /// Queues a skill queue update to be batched.
        /// </summary>
        /// <param name="character">The character whose skill queue was updated.</param>
        public void QueueSkillQueueUpdate(Character character)
        {
            if (_disposed || character == null)
                return;

            lock (_lock)
            {
                _pendingSkillQueueUpdates.Add(character);
                EnsureTimerStarted();
            }
        }

        /// <summary>
        /// Immediately flushes all pending updates without waiting for the coalesce window.
        /// </summary>
        public void FlushNow()
        {
            if (_disposed)
                return;

            FlushInternal();
        }

        /// <summary>
        /// Gets the number of pending character updates.
        /// </summary>
        public int PendingCharacterUpdateCount
        {
            get
            {
                lock (_lock)
                {
                    return _pendingCharacterUpdates.Count;
                }
            }
        }

        /// <summary>
        /// Gets the number of pending skill queue updates.
        /// </summary>
        public int PendingSkillQueueUpdateCount
        {
            get
            {
                lock (_lock)
                {
                    return _pendingSkillQueueUpdates.Count;
                }
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Ensures the flush timer is started.
        /// </summary>
        private void EnsureTimerStarted()
        {
            if (_flushTimer == null)
            {
                _flushTimer = new Timer(OnTimerElapsed, null, _coalesceMs, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Called when the coalesce timer elapses.
        /// </summary>
        private void OnTimerElapsed(object state)
        {
            FlushInternal();
        }

        /// <summary>
        /// Flushes all pending updates and fires the batched events.
        /// </summary>
        private void FlushInternal()
        {
            List<Character> characterUpdates = null;
            List<Character> skillQueueUpdates = null;

            lock (_lock)
            {
                // Stop and dispose the timer
                _flushTimer?.Dispose();
                _flushTimer = null;

                // Capture the pending updates
                if (_pendingCharacterUpdates.Count > 0)
                {
                    characterUpdates = _pendingCharacterUpdates.ToList();
                    _pendingCharacterUpdates.Clear();
                }

                if (_pendingSkillQueueUpdates.Count > 0)
                {
                    skillQueueUpdates = _pendingSkillQueueUpdates.ToList();
                    _pendingSkillQueueUpdates.Clear();
                }
            }

            // Fire events outside the lock
            if (characterUpdates != null && characterUpdates.Count > 0)
            {
                CharactersBatchUpdated?.Invoke(this, new CharacterBatchEventArgs(characterUpdates));
            }

            if (skillQueueUpdates != null && skillQueueUpdates.Count > 0)
            {
                SkillQueuesBatchUpdated?.Invoke(this, new CharacterBatchEventArgs(skillQueueUpdates));
            }
        }

        #endregion


        #region IDisposable

        /// <summary>
        /// Disposes the batcher and flushes any pending updates.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Flush any remaining updates
            FlushInternal();

            lock (_lock)
            {
                _flushTimer?.Dispose();
                _flushTimer = null;
                _pendingCharacterUpdates.Clear();
                _pendingSkillQueueUpdates.Clear();
            }
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for batched character updates.
    /// </summary>
    public class CharacterBatchEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the characters that were updated.
        /// </summary>
        public IReadOnlyList<Character> Characters { get; }

        /// <summary>
        /// Gets the count of updated characters.
        /// </summary>
        public int Count => Characters.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterBatchEventArgs"/> class.
        /// </summary>
        /// <param name="characters">The updated characters.</param>
        public CharacterBatchEventArgs(IEnumerable<Character> characters)
        {
            Characters = characters?.ToList().AsReadOnly() ?? new List<Character>().AsReadOnly();
        }
    }
}
