using System;

namespace EVEMon.Common.QueryMonitor
{
    /// <summary>
    /// Interface for character data querying implementations.
    /// Used by CCPCharacter to interact with CharacterQueryOrchestrator.
    /// </summary>
    internal interface ICharacterDataQuerying : IDisposable
    {
        /// <summary>
        /// Gets whether the character sheet monitor has an error.
        /// Used by CCPCharacter.AdornedName to show "(cached)" label.
        /// </summary>
        bool HasCharacterSheetError { get; }

        /// <summary>
        /// Gets whether the character market orders have been queried.
        /// </summary>
        bool CharacterMarketOrdersQueried { get; }

        /// <summary>
        /// Gets whether the character contracts have been queried.
        /// </summary>
        bool CharacterContractsQueried { get; }

        /// <summary>
        /// Gets whether the character industry jobs have been queried.
        /// </summary>
        bool CharacterIndustryJobsQueried { get; }

        /// <summary>
        /// Processes a single tick, driving all character query monitors.
        /// </summary>
        void ProcessTick();
    }
}
