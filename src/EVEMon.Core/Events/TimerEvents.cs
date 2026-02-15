namespace EVEMon.Core.Events
{
    /// <summary>
    /// Published every second via <see cref="EVEMon.Core.Interfaces.IEventAggregator"/>.
    /// Replaces <c>EveMonClient.SecondTick</c> static event.
    /// </summary>
    public sealed class SecondTickEvent
    {
        public static readonly SecondTickEvent Instance = new SecondTickEvent();
    }

    /// <summary>
    /// Published every five seconds via <see cref="EVEMon.Core.Interfaces.IEventAggregator"/>.
    /// Replaces <c>EveMonClient.FiveSecondTick</c> static event.
    /// </summary>
    public sealed class FiveSecondTickEvent
    {
        public static readonly FiveSecondTickEvent Instance = new FiveSecondTickEvent();
    }

    /// <summary>
    /// Published every thirty seconds via <see cref="EVEMon.Core.Interfaces.IEventAggregator"/>.
    /// Replaces <c>EveMonClient.ThirtySecondTick</c> static event.
    /// </summary>
    public sealed class ThirtySecondTickEvent
    {
        public static readonly ThirtySecondTickEvent Instance = new ThirtySecondTickEvent();
    }
}
