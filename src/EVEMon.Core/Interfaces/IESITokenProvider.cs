using System.Collections.Generic;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides ESI OAuth tokens for API access without depending on character models.
    /// Replaces direct dependency on <c>CCPCharacter</c> for token retrieval.
    /// </summary>
    public interface IESITokenProvider
    {
        /// <summary>
        /// Gets a single token that has the specified scope.
        /// </summary>
        /// <param name="scope">The ESI scope identifier.</param>
        /// <returns>A token result, or null if no token has the requested scope.</returns>
        ESITokenResult? GetTokenForScope(int scope);

        /// <summary>
        /// Gets all tokens that have the specified scope.
        /// </summary>
        /// <param name="scope">The ESI scope identifier.</param>
        /// <returns>All token results that have the requested scope.</returns>
        IEnumerable<ESITokenResult> GetAllTokensForScope(int scope);
    }

    /// <summary>
    /// Represents an ESI access token with associated character identity.
    /// </summary>
    public readonly struct ESITokenResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ESITokenResult"/> struct.
        /// </summary>
        /// <param name="accessToken">The OAuth access token.</param>
        /// <param name="characterId">The character's EVE Online ID.</param>
        /// <param name="characterName">The character's name.</param>
        public ESITokenResult(string accessToken, long characterId, string characterName)
        {
            AccessToken = accessToken;
            CharacterID = characterId;
            CharacterName = characterName;
        }

        /// <summary>
        /// Gets the OAuth access token.
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Gets the character's EVE Online ID.
        /// </summary>
        public long CharacterID { get; }

        /// <summary>
        /// Gets the character's name.
        /// </summary>
        public string CharacterName { get; }
    }
}
