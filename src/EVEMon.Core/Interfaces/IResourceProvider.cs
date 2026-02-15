namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides access to embedded resources (XSLT transforms, static data)
    /// without coupling to a specific Properties.Resources class.
    /// Breaks Data/ -> Properties.Resources dependency (7 call sites, 6 files).
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// Gets the XSLT transform used for datafile deserialization.
        /// </summary>
        string DatafilesXSLT { get; }

        /// <summary>
        /// Gets the CSV data for NPC factions (chrFactions).
        /// </summary>
        string ChrFactions { get; }
    }
}
