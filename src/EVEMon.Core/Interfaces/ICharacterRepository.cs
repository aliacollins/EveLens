namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts access to the character collection.
    /// Replaces direct dependency on <c>EveMonClient.Characters</c> and
    /// <c>EveMonClient.MonitoredCharacters</c>.
    /// Combines <see cref="ICharacterReader"/> and <see cref="ICharacterWriter"/>
    /// for backward compatibility. New code should depend on the specific interface needed.
    /// </summary>
    public interface ICharacterRepository : ICharacterReader, ICharacterWriter
    {
    }
}
