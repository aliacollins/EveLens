namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Resolves EVE inventory flag IDs to display text and vice versa.
    /// Breaks Model -> EveFlag Service dependency (5 call sites, 2 files).
    /// </summary>
    public interface IFlagResolver
    {
        /// <summary>
        /// Gets the display text for a flag ID.
        /// </summary>
        /// <param name="flagId">The inventory flag ID.</param>
        /// <returns>The flag display text.</returns>
        string GetFlagText(int flagId);

        /// <summary>
        /// Gets the flag ID for a flag name.
        /// </summary>
        /// <param name="flagName">The flag name.</param>
        /// <returns>The flag ID.</returns>
        int GetFlagID(string flagName);
    }
}
