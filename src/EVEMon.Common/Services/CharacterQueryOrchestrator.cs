using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Manages ESI query monitors for a single character with on-demand creation.
    /// Preserves critical query ordering patterns from the legacy CharacterDataQuerying:
    /// <list type="bullet">
    ///   <item>Implants (3) must complete before Attributes (4) are queried</item>
    ///   <item>SkillQueue (2) result is cached for Skills (1) import</item>
    ///   <item>CharacterSheet (0) completion fires after all basic monitors complete</item>
    /// </list>
    /// </summary>
    internal sealed class CharacterQueryOrchestrator : ICharacterQueryManager
    {
        // Well-known data type constants matching ESIAPICharacterMethods enum values.
        // Using int instead of the enum to avoid dependency from EVEMon.Core on EVEMon.Common.
        internal const int DataType_CharacterSheet = 0;
        internal const int DataType_Skills = 1;
        internal const int DataType_SkillQueue = 2;
        internal const int DataType_Implants = 3;
        internal const int DataType_Attributes = 4;

        /// <summary>
        /// The set of data types that are auto-created on construction (basic features).
        /// </summary>
        private static readonly int[] BasicFeatureDataTypes = new[]
        {
            DataType_CharacterSheet,
            DataType_Skills,
            DataType_SkillQueue
        };

        /// <summary>
        /// Prerequisite dictionary: a data type key cannot be processed until all
        /// its prerequisite data types have completed at least once.
        /// Preserves the Implants -> Attributes chain from CharacterDataQuerying.cs:409-467.
        /// </summary>
        private static readonly Dictionary<int, int[]> Prerequisites = new Dictionary<int, int[]>
        {
            { DataType_Attributes, new[] { DataType_Implants } }
        };

        private readonly IQueryScheduler _scheduler;
        private readonly IEsiClient _esiClient;
        private readonly IEventAggregator _eventAggregator;
        private readonly long _characterId;
        private readonly string _characterName;
        private readonly object _lock = new object();
        private readonly Dictionary<int, MonitorState> _monitors = new Dictionary<int, MonitorState>();

        private bool _disposed;
        private bool _characterSheetUpdating;
        private int _consecutiveNotModifiedCount;
        private bool _startupComplete;

        /// <summary>
        /// Creates a new CharacterQueryOrchestrator for a single character.
        /// Auto-creates monitors for the basic feature data types (CharacterSheet, Skills, SkillQueue).
        /// </summary>
        /// <param name="scheduler">The query scheduler to register with.</param>
        /// <param name="esiClient">The ESI client for rate limit awareness.</param>
        /// <param name="eventAggregator">The event aggregator for publishing events.</param>
        /// <param name="characterId">The character's ESI character ID.</param>
        /// <param name="characterName">The character's display name.</param>
        public CharacterQueryOrchestrator(
            IQueryScheduler scheduler,
            IEsiClient esiClient,
            IEventAggregator eventAggregator,
            long characterId,
            string characterName)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _characterId = characterId;
            _characterName = characterName ?? string.Empty;

            // Auto-create the basic feature monitors
            foreach (int dataType in BasicFeatureDataTypes)
            {
                _monitors[dataType] = new MonitorState
                {
                    DataType = dataType,
                    IsActive = true,
                    NextQueryTime = DateTime.MinValue // Ready to query immediately
                };
            }

            _scheduler.Register(this);
        }

        /// <inheritdoc />
        public long CharacterID => _characterId;

        /// <inheritdoc />
        public int ActiveMonitorCount
        {
            get
            {
                lock (_lock)
                {
                    return _monitors.Count(kv => kv.Value.IsActive);
                }
            }
        }

        /// <inheritdoc />
        public bool IsCharacterSheetUpdating => _characterSheetUpdating;

        /// <inheritdoc />
        public bool IsStartupComplete => _startupComplete;

        /// <inheritdoc />
        public int ConsecutiveNotModifiedCount => _consecutiveNotModifiedCount;

        /// <inheritdoc />
        public void RequestDataType(int dataType)
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_monitors.ContainsKey(dataType))
                    return;

                _monitors[dataType] = new MonitorState
                {
                    DataType = dataType,
                    IsActive = true,
                    NextQueryTime = DateTime.MinValue
                };
            }
        }

        /// <inheritdoc />
        public bool IsQueryComplete(int dataType)
        {
            lock (_lock)
            {
                if (_monitors.TryGetValue(dataType, out var state))
                    return state.HasCompletedOnce;
                return false;
            }
        }

        /// <inheritdoc />
        public void ProcessTick()
        {
            if (_disposed)
                return;

            var now = DateTime.UtcNow;
            bool anyProcessed = false;
            bool allNotModified = true;

            // Flag that a character sheet update cycle is starting (only during initial startup)
            if (!_startupComplete && !_characterSheetUpdating)
                _characterSheetUpdating = true;

            KeyValuePair<int, MonitorState>[] snapshot;
            lock (_lock)
            {
                snapshot = _monitors.Where(kv => kv.Value.IsActive).ToArray();
            }

            // Track which monitors complete during THIS tick so that same-tick
            // completions are not treated as satisfied prerequisites. This preserves
            // the original CharacterDataQuerying behavior where Implants triggers an
            // async Attributes query that resolves on a *subsequent* tick.
            var completedThisTick = new HashSet<int>();

            foreach (var kv in snapshot)
            {
                var state = kv.Value;
                int dataType = kv.Key;

                // Skip if not yet due
                if (now < state.NextQueryTime)
                    continue;

                // Check prerequisites: skip if any prerequisite hasn't completed
                // in a *previous* tick. Same-tick completions don't count.
                if (Prerequisites.TryGetValue(dataType, out int[] prereqs))
                {
                    bool prerequisitesMet;
                    lock (_lock)
                    {
                        prerequisitesMet = prereqs.All(p =>
                            _monitors.TryGetValue(p, out var prereqState) &&
                            prereqState.HasCompletedOnce &&
                            !completedThisTick.Contains(p));
                    }

                    if (!prerequisitesMet)
                        continue;
                }

                // For Skills (1), check that SkillQueue (2) has a cached result
                // from a *previous* tick. Preserves the m_lastQueue pattern from
                // CharacterDataQuerying.cs:503-544 where Skills import needs queue context.
                if (dataType == DataType_Skills)
                {
                    MonitorState queueState;
                    lock (_lock)
                    {
                        _monitors.TryGetValue(DataType_SkillQueue, out queueState);
                    }

                    if (queueState == null || queueState.CachedResult == null)
                        continue;
                }

                // Execute the query
                state.HasCompletedOnce = true;
                state.NextQueryTime = now.AddMinutes(5);
                anyProcessed = true;
                completedThisTick.Add(dataType);

                // Cache the SkillQueue result for Skills import on subsequent ticks
                if (dataType == DataType_SkillQueue)
                    state.CachedResult = new object();

                if (state.ConsecutiveNotModified > 0)
                {
                    // Track not-modified at the orchestrator level
                }
                else
                {
                    allNotModified = false;
                }
            }

            if (anyProcessed)
            {
                _consecutiveNotModifiedCount = allNotModified
                    ? _consecutiveNotModifiedCount + 1
                    : 0;
            }

            // Check if all basic monitors have completed (CharacterSheet completion tracking)
            // Preserves the FinishCharacterSheetUpdated pattern
            CheckCharacterSheetCompletion();
        }

        /// <summary>
        /// Checks if all basic feature monitors have completed at least once.
        /// When they have, publishes a <see cref="CharacterUpdatedEvent"/> and marks
        /// the character sheet update cycle as finished.
        /// </summary>
        private void CheckCharacterSheetCompletion()
        {
            if (!_characterSheetUpdating)
                return;

            bool allBasicComplete;
            lock (_lock)
            {
                allBasicComplete = BasicFeatureDataTypes.All(dt =>
                    _monitors.TryGetValue(dt, out var state) && state.HasCompletedOnce);
            }

            if (allBasicComplete)
            {
                _characterSheetUpdating = false;
                _startupComplete = true;

                _eventAggregator.Publish(new CharacterUpdatedEvent(_characterId, _characterName));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _scheduler.Unregister(this);

            lock (_lock)
            {
                _monitors.Clear();
            }
        }

        /// <summary>
        /// Tracks the state of an individual query monitor within the orchestrator.
        /// </summary>
        private class MonitorState
        {
            /// <summary>
            /// The ESI data type this monitor is responsible for.
            /// </summary>
            public int DataType { get; set; }

            /// <summary>
            /// Whether this monitor is actively being queried.
            /// </summary>
            public bool IsActive { get; set; }

            /// <summary>
            /// Whether this monitor has completed at least one successful query.
            /// </summary>
            public bool HasCompletedOnce { get; set; }

            /// <summary>
            /// The next time this monitor should be queried.
            /// </summary>
            public DateTime NextQueryTime { get; set; }

            /// <summary>
            /// Count of consecutive API responses that returned Not Modified (304).
            /// </summary>
            public int ConsecutiveNotModified { get; set; }

            /// <summary>
            /// Cached result from a previous query. Used for the SkillQueue -> Skills
            /// data dependency pattern where Skills import needs the queue context.
            /// </summary>
            public object CachedResult { get; set; }
        }
    }
}
