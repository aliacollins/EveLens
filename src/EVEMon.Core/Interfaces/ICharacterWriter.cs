namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides write (mutation) operations for the character collection.
    /// Separated from <see cref="ICharacterReader"/> so that consumers that only need to
    /// query characters do not see or depend on mutation methods.
    /// </summary>
    /// <remarks>
    /// Mutations delegate to <c>GlobalMonitoredCharacterCollection.OnCharacterMonitoringChanged()</c>,
    /// which fires the <c>MonitoredCharacterCollectionChanged</c> event on <c>EveMonClient</c>.
    ///
    /// Production: <c>CharacterRepositoryService</c> in <c>EVEMon.Common/Services/CharacterRepositoryService.cs</c>
    /// (via the combined <see cref="ICharacterRepository"/> interface).
    /// Testing: Implement with a simple in-memory set of monitored GUIDs.
    /// </remarks>
    public interface ICharacterWriter
    {
        /// <summary>
        /// Adds or removes a character from the monitored set.
        /// Setting <paramref name="value"/> to true starts ESI polling for the character;
        /// false stops it. Does nothing if <paramref name="character"/> is null.
        /// </summary>
        /// <param name="character">The character identity to update.</param>
        /// <param name="value">True to begin monitoring, false to stop monitoring.</param>
        void SetMonitored(ICharacterIdentity character, bool value);
    }
}
