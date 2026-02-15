namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Represents a station or structure in EVE Online.
    /// Provides a type-safe interface for station information,
    /// eliminating the need for casting from object.
    /// </summary>
    public interface IStation
    {
        /// <summary>
        /// Gets the station or structure ID.
        /// </summary>
        long ID { get; }

        /// <summary>
        /// Gets the station name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the solar system ID where this station is located.
        /// </summary>
        int SolarSystemID { get; }
    }
}
