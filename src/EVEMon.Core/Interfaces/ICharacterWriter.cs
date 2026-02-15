namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides write operations for the character collection.
    /// Separated from <see cref="ICharacterReader"/> to allow consumers
    /// to depend only on the operations they need.
    /// </summary>
    public interface ICharacterWriter
    {
        /// <summary>
        /// Sets whether the specified character is monitored.
        /// </summary>
        /// <param name="character">The character identity to update.</param>
        /// <param name="value">True to monitor, false to unmonitor.</param>
        void SetMonitored(ICharacterIdentity character, bool value);
    }
}
