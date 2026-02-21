// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Minimal character identity contract for use in core interfaces and cross-layer communication.
    /// Avoids dependency on the full <c>Character</c> model hierarchy in <c>EVEMon.Common</c>.
    /// </summary>
    /// <remarks>
    /// Implemented by the <c>Character</c> base class (and therefore by <c>CCPCharacter</c>
    /// and <c>UriCharacter</c>). This allows Core-level code (ServiceLocator, event classes,
    /// factory interfaces) to reference characters without pulling in the entire Common assembly.
    ///
    /// The <see cref="Guid"/> property is the application-internal unique identifier (not the
    /// EVE character ID). It is stable across sessions and used for settings serialization.
    /// The <see cref="CharacterID"/> is the EVE Online character ID from ESI.
    ///
    /// Production: Implemented by <c>Character</c> in <c>EVEMon.Common/Models/Character.cs</c>.
    /// Testing: Create a simple record or class with these four properties.
    /// </remarks>
    public interface ICharacterIdentity
    {
        /// <summary>
        /// Gets the application-internal unique identifier for this character.
        /// Stable across sessions; used as the key in settings serialization.
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// Gets the character's display name as shown in the UI.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the character's EVE Online ID (from ESI). Zero for characters that
        /// have not yet been authenticated via SSO.
        /// </summary>
        long CharacterID { get; }

        /// <summary>
        /// Gets a value indicating whether this character is actively monitored.
        /// Monitored characters have their ESI data polled on the scheduler tick cycle.
        /// </summary>
        bool Monitored { get; }
    }
}
