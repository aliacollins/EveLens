// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Provides ESI OAuth2 access tokens for authenticated API calls without depending
    /// on the <c>CCPCharacter</c> model. Allows services that need ESI tokens (e.g.,
    /// citadel lookups, market data) to obtain them without coupling to the character layer.
    /// Replaces direct dependency on iterating <c>CCPCharacter</c> instances for token access.
    /// </summary>
    /// <remarks>
    /// Each ESI key/character has a set of granted scopes. The provider selects tokens
    /// that have the requested scope, enabling callers to find a character with the right
    /// permissions for a specific ESI endpoint.
    ///
    /// The scope parameter is an <c>int</c> (cast from <c>ESIAPICharacterMethods</c>)
    /// because the Core assembly cannot reference the enum defined in <c>EveLens.Common</c>.
    ///
    /// Production: Implement by iterating <c>AppServices.ESIKeys</c> and checking scope grants.
    /// Testing: Provide a stub returning canned <see cref="ESITokenResult"/> values.
    /// </remarks>
    public interface IESITokenProvider
    {
        /// <summary>
        /// Gets a single token that has the specified ESI scope.
        /// Returns the first matching token found, or null if no character has the scope.
        /// </summary>
        /// <param name="scope">The ESI scope identifier (cast from <c>ESIAPICharacterMethods</c>).</param>
        /// <returns>A token result with the access token and character info, or null if unavailable.</returns>
        ESITokenResult? GetTokenForScope(int scope);

        /// <summary>
        /// Gets all tokens from all characters that have the specified ESI scope.
        /// Useful when multiple characters may have access to a structure and the caller
        /// wants to try each token until one succeeds.
        /// </summary>
        /// <param name="scope">The ESI scope identifier (cast from <c>ESIAPICharacterMethods</c>).</param>
        /// <returns>All token results that have the requested scope (may be empty).</returns>
        IEnumerable<ESITokenResult> GetAllTokensForScope(int scope);
    }

    /// <summary>
    /// Represents an ESI OAuth2 access token paired with the character identity it belongs to.
    /// Immutable value type for efficient passing without allocation.
    /// </summary>
    public readonly struct ESITokenResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ESITokenResult"/> struct.
        /// </summary>
        /// <param name="accessToken">The OAuth2 access token string for ESI API calls.</param>
        /// <param name="characterId">The EVE Online character ID that owns this token.</param>
        /// <param name="characterName">The display name of the character that owns this token.</param>
        public ESITokenResult(string accessToken, long characterId, string characterName)
        {
            AccessToken = accessToken;
            CharacterID = characterId;
            CharacterName = characterName;
        }

        /// <summary>
        /// Gets the OAuth2 access token string for authenticating ESI API requests.
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the EVE Online character ID that owns this token.
        /// </summary>
        public long CharacterID { get; }

        /// <summary>
        /// Gets the display name of the character that owns this token.
        /// </summary>
        public string CharacterName { get; }
    }
}
