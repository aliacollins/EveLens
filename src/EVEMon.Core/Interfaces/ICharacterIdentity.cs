using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Minimal character identity for use in core interfaces.
    /// Avoids dependency on the full <c>Character</c> model hierarchy.
    /// </summary>
    public interface ICharacterIdentity
    {
        /// <summary>
        /// Gets the character's unique identifier.
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// Gets the character's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the character's EVE Online ID.
        /// </summary>
        long CharacterID { get; }

        /// <summary>
        /// Gets a value indicating whether this character is actively monitored.
        /// </summary>
        bool Monitored { get; }
    }
}
