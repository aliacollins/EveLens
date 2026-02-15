using System;

namespace EVEMon.Common.QueryMonitor
{
    /// <summary>
    /// Interface for corporation data querying implementations.
    /// Used by CCPCharacter to interact with CorporationQueryOrchestrator.
    /// </summary>
    public interface ICorporationDataQuerying : IDisposable
    {
        /// <summary>
        /// Gets whether the corporation market orders have been queried.
        /// </summary>
        bool CorporationMarketOrdersQueried { get; }

        /// <summary>
        /// Gets whether the corporation contracts have been queried.
        /// </summary>
        bool CorporationContractsQueried { get; }

        /// <summary>
        /// Gets whether the corporation industry jobs have been queried.
        /// </summary>
        bool CorporationIndustryJobsQueried { get; }

        /// <summary>
        /// Processes a single tick, driving all corporation query monitors.
        /// </summary>
        void ProcessTick();
    }
}
