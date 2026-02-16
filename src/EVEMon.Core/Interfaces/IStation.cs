namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Represents a station or player-owned structure in EVE Online.
    /// Provides a type-safe contract for station information, eliminating the need for
    /// callers in the Core layer to cast from <c>object</c> when working with stations
    /// returned by <see cref="IStationResolver"/>.
    /// </summary>
    /// <remarks>
    /// Covers both NPC stations (loaded from static data) and player-owned structures/citadels
    /// (resolved via ESI). The <see cref="SolarSystemID"/> links to the solar system in the
    /// static universe data.
    ///
    /// Production: Implemented by <c>Station</c> in <c>EVEMon.Common/Data/Station.cs</c>.
    /// The <c>Station</c> class also implements <c>IComparable&lt;Station&gt;</c> and extends
    /// <c>ReadonlyCollection&lt;Agent&gt;</c> for agent data.
    /// Testing: Create a simple record with these three properties.
    /// </remarks>
    public interface IStation
    {
        /// <summary>
        /// Gets the station or structure ID (NPC stations use CCP-assigned IDs;
        /// citadels use 64-bit structure IDs).
        /// </summary>
        long ID { get; }

        /// <summary>
        /// Gets the station's display name. For citadels, this is the player-assigned name.
        /// May be "Inaccessible Structure" if the ESI lookup has not yet completed or the
        /// character lacks docking rights.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the solar system ID where this station is located.
        /// Zero if the station's location is unknown (e.g., inaccessible citadel).
        /// </summary>
        int SolarSystemID { get; }
    }
}
