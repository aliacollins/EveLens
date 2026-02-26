// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Provides unified read/write access to the character collection.
    /// Replaces direct dependency on <c>EveLensClient.Characters</c> and
    /// <c>EveLensClient.MonitoredCharacters</c> static collections.
    /// Combines <see cref="ICharacterReader"/> and <see cref="ICharacterWriter"/>
    /// for backward compatibility; new code should depend on the narrower interface it needs.
    /// </summary>
    /// <remarks>
    /// The underlying collection is the <c>GlobalCharacterCollection</c> owned by <c>EveLensClient</c>.
    /// Read operations snapshot into <c>IReadOnlyList</c> copies; writes delegate to the real
    /// collection methods.
    ///
    /// Production: <c>CharacterRepositoryService</c> in <c>EveLens.Common/Services/CharacterRepositoryService.cs</c>.
    /// Testing: Implement the interface with an in-memory list, or use <see cref="ICharacterReader"/>
    /// / <see cref="ICharacterWriter"/> separately for finer-grained test doubles.
    /// </remarks>
    public interface ICharacterRepository : ICharacterReader, ICharacterWriter
    {
    }
}
