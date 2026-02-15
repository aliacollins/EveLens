using System;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Adapts query orchestrators to <see cref="IScheduledQueryable"/>
    /// so <see cref="SmartQueryScheduler"/> can drive them.
    /// </summary>
    internal sealed class ScheduledQueryableAdapter : IScheduledQueryable
    {
        private readonly Action _processTick;

        /// <inheritdoc />
        public long CharacterID { get; }

        /// <summary>
        /// Always returns true because the wrapped objects handle their own startup delay internally.
        /// SmartQueryScheduler's staggered startup would double-delay if we reported false.
        /// </summary>
        public bool IsStartupComplete => true;

        /// <summary>
        /// Always returns 0 because the wrapped objects don't track ESI Not-Modified responses
        /// at this level. Individual monitors within the orchestrator handle their own
        /// cache expiry via ETag/If-None-Match headers.
        /// </summary>
        public int ConsecutiveNotModifiedCount => 0;

        /// <summary>
        /// Creates a new adapter wrapping a ProcessTick action.
        /// </summary>
        /// <param name="characterId">The character ID associated with this queryable.</param>
        /// <param name="processTick">The action to invoke on each tick.</param>
        public ScheduledQueryableAdapter(long characterId, Action processTick)
        {
            CharacterID = characterId;
            _processTick = processTick ?? throw new ArgumentNullException(nameof(processTick));
        }

        /// <inheritdoc />
        public void ProcessTick() => _processTick();
    }
}
