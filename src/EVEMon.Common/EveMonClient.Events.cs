using EVEMon.Common.Helpers;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common
{
    public static partial class EveMonClient
    {
        #region Timer Tick Counter

        /// <summary>
        /// Counter for tiered timer system.
        /// </summary>
        private static int s_tickCounter;

        /// <summary>
        /// Re-entrancy guard for timer tick processing.
        /// Prevents cascading ticks when processing takes longer than 1 second
        /// (common with 60+ characters where hundreds of event handlers fire).
        /// </summary>
        private static bool s_tickProcessing;

        #endregion

        #region Timer Tick

        /// <summary>
        /// Fires the timer tick event to notify the subscribers.
        /// Uses tiered system to reduce overhead for 100+ character scenarios.
        /// </summary>
        internal static void UpdateOnOneSecondTick()
        {
            if (Closed)
                return;

            // Re-entrancy guard: if the previous tick is still processing (common with 60+
            // characters where hundreds of handlers fire synchronously), skip this tick.
            // The DispatcherTimer will fire again in 1 second and we'll catch up then.
            if (s_tickProcessing)
                return;

            s_tickProcessing = true;
            try
            {
                // Increment tick counter
                s_tickCounter++;

                // Fire tiered events
                // SecondTick - every 1 second (skill countdowns, visible UI)
                AppServices.EventAggregator?.Publish(SecondTickEvent.Instance);

                // FiveSecondTick - every 5 seconds (API checks, cache expiry)
                if (s_tickCounter % 5 == 0)
                {
                    AppServices.EventAggregator?.Publish(FiveSecondTickEvent.Instance);
                    AppServices.TraceService?.Trace("[TICK] 5s fired", printMethod: false);
                }

                // ThirtySecondTick - every 30 seconds (background tasks)
                if (s_tickCounter % 30 == 0)
                {
                    AppServices.EventAggregator?.Publish(ThirtySecondTickEvent.Instance);
                    AppServices.TraceService?.Trace("[TICK] 30s fired", printMethod: false);
                    s_tickCounter = 0; // Reset to prevent overflow
                }
            }
            finally
            {
                s_tickProcessing = false;
            }
        }

        #endregion


        #region Event Coalescing

        /// <summary>
        /// Called when the update batcher has collected character updates ready to fire.
        /// </summary>
        private static void OnBatchedCharacterUpdatesReady(object? sender, CharacterBatchEventArgs e)
        {
            if (Closed)
                return;

            Trace($"Batched update for {e.Count} characters");

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharactersBatchUpdatedEvent(e.Characters));
        }

        /// <summary>
        /// Called when the update batcher has collected skill queue updates ready to fire.
        /// </summary>
        private static void OnBatchedSkillQueueUpdatesReady(object? sender, CharacterBatchEventArgs e)
        {
            if (Closed)
                return;

            Trace($"Batched skill queue update for {e.Count} characters");

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.SkillQueuesBatchUpdatedEvent(e.Characters));
        }

        #endregion

    }
}
