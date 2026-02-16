namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides unified read/write access to the character collection.
    /// Replaces direct dependency on <c>EveMonClient.Characters</c> and
    /// <c>EveMonClient.MonitoredCharacters</c> static collections.
    /// Combines <see cref="ICharacterReader"/> and <see cref="ICharacterWriter"/>
    /// for backward compatibility; new code should depend on the narrower interface it needs.
    /// </summary>
    /// <remarks>
    /// The underlying collection is the <c>GlobalCharacterCollection</c> owned by <c>EveMonClient</c>.
    /// Read operations snapshot into <c>IReadOnlyList</c> copies; writes delegate to the real
    /// collection methods.
    ///
    /// Production: <c>CharacterRepositoryService</c> in <c>EVEMon.Common/Services/CharacterRepositoryService.cs</c>.
    /// Testing: Implement the interface with an in-memory list, or use <see cref="ICharacterReader"/>
    /// / <see cref="ICharacterWriter"/> separately for finer-grained test doubles.
    /// </remarks>
    public interface ICharacterRepository : ICharacterReader, ICharacterWriter
    {
    }
}
