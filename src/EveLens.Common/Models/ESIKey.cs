// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;
using EveLens.Common.Constants;
using EveLens.Common.CustomEventArgs;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Extensions;
using EveLens.Common.Helpers;
using EveLens.Common.Interfaces;
using EveLens.Common.Net;
using EveLens.Common.QueryMonitor;
using EveLens.Common.Serialization;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Service;
using EveLens.Common.Services;
using CoreEvents = EveLens.Core.Events;
using CommonEvents = EveLens.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace EveLens.Common.Models
{
    /// <summary>
    /// Represents a player ESI key.
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class ESIKey
    {
        #region Fields

        // When account status returns...
        //private readonly APIKeyQueryMonitor<SerializableAPIAccountStatus> m_accountStatusMonitor;

        private bool m_monitored;
        private bool m_queried;
        private bool m_queryPending;
        private DateTime m_keyExpires;

        #endregion


        #region Constructors

        /// <summary>
        /// Common constructor base.
        /// </summary>
        private ESIKey()
        {
            m_keyExpires = DateTime.MinValue;
            m_queried = false;
            m_queryPending = false;
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="serial">The serialized key.</param>
        internal ESIKey(SerializableESIKey serial)
            : this()
        {
            ID = serial.ID;
            m_monitored = serial.Monitored;
            RefreshToken = serial.RefreshToken ?? string.Empty;

            // Load authorized scopes; migrate from legacy AccessMask if needed
            if (serial.AuthorizedScopes != null && serial.AuthorizedScopes.Count > 0)
            {
                _authorizedScopes = new List<string>(serial.AuthorizedScopes);
            }
#pragma warning disable CS0618 // AccessMask is obsolete
            else if (serial.AccessMask == ulong.MaxValue)
            {
                // Legacy key with full access — default to all scopes
                _authorizedScopes = new List<string>(Services.EsiScopePresets.AllScopes);
            }
#pragma warning restore CS0618
            // else: empty scopes (invalid or zero-mask key)
        }

        /// <summary>
        /// Constructor from the provided informations.
        /// </summary>
        /// <param name="id">The ESI key ID (not too terribly meaningful).</param>
        public ESIKey(long id)
            : this()
        {
            ID = id;
            RefreshToken = string.Empty;
            m_monitored = true;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        public long ID { get; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        [XmlIgnore]
        public string AccessToken { get; private set; }

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        /// <value>The refresh token.</value>
        [XmlIgnore]
        public string RefreshToken { get; private set; }

        /// <summary>
        /// The ESI scopes that were granted when this key was authenticated.
        /// </summary>
        private List<string> _authorizedScopes = new();

        /// <summary>
        /// Gets the ESI scopes that were granted when this key was authenticated.
        /// </summary>
        [XmlIgnore]
        public IReadOnlyList<string> AuthorizedScopes => _authorizedScopes;

        /// <summary>
        /// Returns true if this key includes the specified ESI scope.
        /// </summary>
        public bool HasScope(string scope) => _authorizedScopes.Contains(scope);

        /// <summary>
        /// Returns true if this key has the scope required for the given character API method.
        /// Returns true for public endpoints that require no scope.
        /// </summary>
        public bool HasAccessTo(ESIAPICharacterMethods method)
            => Constants.EsiScopeMapping.HasScope(_authorizedScopes, method);

        /// <summary>
        /// Returns true if this key has the scope required for the given corporation API method.
        /// Returns true for public endpoints that require no scope.
        /// </summary>
        public bool HasAccessTo(ESIAPICorporationMethods method)
            => Constants.EsiScopeMapping.HasCorpScope(_authorizedScopes, method);

        /// <summary>
        /// Returns true if an error occurred while last trying to refresh this key.
        /// </summary>
        [XmlIgnore]
        public bool HasError { get; internal set; }

        /// <summary>
        /// True when an SSO token refresh is in-flight and AccessToken may be empty.
        /// Query executors should skip (and retry shortly) rather than send requests
        /// with an empty bearer token.
        /// </summary>
        [XmlIgnore]
        public bool IsTokenRefreshing => m_queryPending;

#if false
        /// <summary>
        /// Gets the account expiration date and time. RIP Account status API.
        /// </summary>
        public DateTime AccountExpires { get; set; }
#endif

        /// <summary>
        /// Gets the character identities for this API key.
        /// </summary>
        public IEnumerable<CharacterIdentity> CharacterIdentities => EveLensClient.
            CharacterIdentities.Where(characterID => characterID.ESIKeys.Contains(this));
        
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ESIKey"/> is monitored.
        /// </summary>
        /// <value><c>true</c> if monitored; otherwise, <c>false</c>.</value>
        public bool Monitored
        {
            get { return m_monitored; }
            set
            {
                m_monitored = value;
                AppServices.TraceService?.Trace(ToString());
                AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyMonitoredChangedEvent.Instance);
            }
        }

        /// <summary>
        /// Gets true if at least one of the CCP characters is monitored.
        /// </summary>
        public bool HasMonitoredCharacters => CharacterIdentities.Select(id => id.CCPCharacter)
            .Any(ccpCharacter => ccpCharacter != null && ccpCharacter.Monitored);

        /// <summary>
        /// Gets the character in training on this API key, or null if none are in training.
        /// </summary>
        /// <remarks>Returns null if the character is in the ignored list.</remarks>
        // Scroll through owned identities
        public CCPCharacter TrainingCharacter => CharacterIdentities.Select(id => id.CCPCharacter)
            .FirstOrDefault(ccpCharacter => ccpCharacter != null && ccpCharacter.IsTraining);

        /// <summary>
        /// Gets true if this API key has a character in training.
        /// </summary>
        public bool HasCharacterInTraining => TrainingCharacter != null;
        
        /// <summary>
        /// Gets true if this API key got queried or is not monitored.
        /// </summary>
        public bool IsProcessed => m_queried || !m_monitored;

        #endregion


        #region Internal Methods
        
        /// <summary>
        /// Clears the refresh token. Used after settings restore to prevent
        /// auto-refresh attempts with stale PKCE tokens.
        /// </summary>
        internal void ClearRefreshToken()
        {
            RefreshToken = string.Empty;
        }

        /// <summary>
        /// Starts obtaining an access token from the refresh token, because either the access
        /// token expired or was never obtained.
        /// </summary>
        internal void CheckAccessToken()
        {
            var rt = RefreshToken;
            if (m_keyExpires < DateTime.UtcNow && !string.IsNullOrEmpty(rt) && !m_queryPending)
            {
                var auth = SSOAuthenticationService.GetInstance();
                if (auth == null)
                    // User removed the client ID / secret
                    HasError = true;
                else
                {
                    auth.GetNewToken(rt, OnAccessToken);
                    m_queryPending = true;
                }
            }
        }

        /// <summary>
        /// Updates the access token, or sets an error flag if the token could no longer be
        /// obtained.
        /// </summary>
        /// <param name="response">The token response received from the server.</param>
        private void OnAccessToken(AccessResponse response)
        {
            m_queried = true;

            if (response == null)
            {
                // If it errors out, avoid checking again for another 5 minutes
                m_keyExpires = DateTime.UtcNow.AddMinutes(5.0);
                AppServices.Notifications.NotifySSOError();
                HasError = true;
                m_queryPending = false;
                AppServices.TraceService?.Trace(ToString());
                AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyInfoUpdatedEvent.Instance);
            }
            else
            {
                AccessToken = response.AccessToken;
                // PKCE routinely updates refresh tokens
                RefreshToken = response.RefreshToken;
                m_keyExpires = response.ExpiryUTC;
                // Have to make a second request for the character information!
                SSOAuthenticationService.GetTokenInfo(AccessToken, OnTokenInfo);
            }
        }

        /// <summary>
        /// Updates the token character list, or sets an error flag if it could not be
        /// obtained.
        /// </summary>
        private void OnTokenInfo(JsonResult<EsiAPITokenInfo> result)
        {
            EsiAPITokenInfo tokenInfo = result.Result;
            if (result.HasError)
            {
                HasError = true;
                AppServices.Notifications.NotifyCharacterListError(this, result);
            }
            else
            {
                HasError = false;
                ImportIdentities(tokenInfo);
                AppServices.Notifications.InvalidateAPIError();

                // Notify health tracker that token refresh succeeded — resets Suspended state
                foreach (var identity in CharacterIdentities)
                {
                    if (identity.CCPCharacter != null)
                        AppServices.EventAggregator?.Publish(
                            new Core.Events.ESIKeyTokenRefreshedEvent(identity.CCPCharacter.CharacterID));
                }
            }
            m_queryPending = false;
            AppServices.TraceService?.Trace(ToString());
            AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyInfoUpdatedEvent.Instance);
        }

        #endregion


        #region Inherited events

        /// <summary>
        /// Called when the object gets disposed.
        /// </summary>
        internal void Dispose()
        {
            // No event subscriptions to unsubscribe - ESIKey token refresh
            // is driven by EveLensClient.OnEsiKeyRefreshTick instead of per-key FiveSecondTick
        }

        #endregion


        #region Global Events

        /// <summary>
        /// Checks the access token for refresh. Called by EveLensClient on FiveSecondTick.
        /// </summary>
        internal void ProcessTick()
        {
            CheckAccessToken();
        }

        #endregion


        #region Queries response

#if false
        /// <summary>
        /// Called when the account status has been updated.
        /// </summary>
        /// <param name="result">The result.</param>
        private void OnAccountStatusUpdated(JsonResult<SerializableAPIAccountStatus> result)
        {
            // Quit if the API key was deleted while it was updating
            if (!AppServices.ESIKeys.Contains(this))
                return;
            
            // Return on error
            if (result.HasError)
            {
                AppServices.Notifications.NotifyAccountStatusError(this, result);
                return;
            }

            AppServices.Notifications.InvalidateAccountStatusError(this);

            // Notifies for the account expiration
            NotifyAccountExpiration();

            // Fires the event regarding the account status update
            AppServices.EventAggregator?.Publish(CommonEvents.AccountStatusUpdatedEvent.Instance);
        }
#endif
        
        #endregion


        #region Helper Methods

        /// <summary>
        /// Forces the update.
        /// </summary>
        public void ForceUpdate()
        {
            CheckAccessToken();
        }
        
        /// <summary>
        /// Notifies for the account expiration.
        /// </summary>
        private void NotifyAccountExpiration()
        {
            // No Account Status API in ESI
#if false
            DateTime AccountExpires = DateTime.MaxValue;

            // Is it to expire within 7 days? Send an informative notification
            TimeSpan daysToExpire = AccountExpires.Subtract(DateTime.UtcNow);
            if (daysToExpire < TimeSpan.FromDays(7) && daysToExpire > TimeSpan.FromDays(1))
            {
                AppServices.Notifications.NotifyAccountExpiration(this, AccountExpires, NotificationPriority.Information);
                return;
            }

            // Is it to expire within the day? Send a warning notification
            if (daysToExpire <= TimeSpan.FromDays(1) && daysToExpire > TimeSpan.Zero)
            {
                AppServices.Notifications.NotifyAccountExpiration(this, AccountExpires, NotificationPriority.Warning);
                return;
            }
#endif
            AppServices.Notifications.InvalidateAccountExpiration(this);
        }
        
        #endregion


        #region Static Methods

        /// <summary>
        /// Tries to add or update the ESI key.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="accessResponse">The access and refresh token.</param>
        /// <param name="callback">The callback.</param>
        public static void TryAddOrUpdateAsync(long id, AccessResponse accessResponse,
                                               EventHandler<ESIKeyCreationEventArgs> callback)
        {
            accessResponse.ThrowIfNull(nameof(accessResponse));
            SSOAuthenticationService.GetTokenInfo(accessResponse.AccessToken,
                (result) => callback(null, new ESIKeyCreationEventArgs(id, accessResponse.
                RefreshToken, result)));
        }
        
        /// <summary>
        /// Check whether some accounts are not in training.
        /// </summary>
        /// <param name="message">Message describing the accounts not in training.</param>
        /// <returns>True if one or more accounts is not in training, otherwise false.</returns>
        /// <remarks>This condition applied only to those API keys of type 'Account'</remarks>
        public static bool HasCharactersNotTraining(out string message)
        {
            message = string.Empty;

            List<ESIKey> accountsNotTraining = AppServices.ESIKeys.Where(
                esiKey => esiKey.CharacterIdentities.Any() && !esiKey.HasCharacterInTraining)
                .ToList();

            // All accounts are training ?
            if (!accountsNotTraining.Any())
                return false;

            // Creates the string, scrolling through every not training account
            StringBuilder builder = new StringBuilder();
            builder.Append(accountsNotTraining.Count == 1
                ? $"{(AppServices.ESIKeys.Count == 1 ? "The account" : "One of the accounts")} is not in training"
                : "Some of the accounts are not in training.");

            foreach (ESIKey esiKey in accountsNotTraining)
            {
                builder.AppendLine().Append($"ESI key : {esiKey}");
            }

            message = builder.ToString();
            return true;
        }

        #endregion


        #region Importation / Exportation
        
        /// <summary>
        /// Updates the characters list with the given CCP data.
        /// </summary>
        /// <param name="tokenInfo">The ESI token info from CCP.</param>
        private void ImportIdentities(EsiAPITokenInfo tokenInfo)
        {
            var chars = AppServices.CharacterIdentities.Where(id => id.ESIKeys.Contains(this));
            long charID = tokenInfo.CharacterID;
            // Clear the API key on this character
            foreach (CharacterIdentity id in chars)
                id.ESIKeys.Remove(this);

            // Find characters who own this ESI key
            // Can match at most one character
            CharacterIdentity cid = AppServices.CharacterIdentities[charID] ??
                AppServices.CharacterIdentities.Add(charID, tokenInfo.CharacterName);
            // Add the ESI key to the identity
            cid.ESIKeys.Add(this);
            if (cid.CCPCharacter != null)
            {
                // Notify subscribers
                AppServices.TraceService?.Trace(cid.CCPCharacter.Name);
                AppServices.EventAggregator?.Publish(new CoreEvents.CharacterUpdatedEvent(cid.CCPCharacter.CharacterID, cid.CCPCharacter.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpdatedEvent(cid.CCPCharacter));
            }

            // Fires the event regarding the character list update
            AppServices.TraceService?.Trace(ToString());
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterListUpdatedEvent(this));
            AppServices.EventAggregator?.Publish(CoreEvents.ESIKeyCollectionChangedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// Exports the data to a serialization object.
        /// </summary>
        /// <returns></returns>
        internal SerializableESIKey Export()
        {
            var serial = new SerializableESIKey
            {
                ID = ID,
                RefreshToken = RefreshToken,
                Monitored = m_monitored,
                AuthorizedScopes = new List<string>(_authorizedScopes),
            };

            return serial;
        }

        #endregion


        #region Update Methods

        /// <summary>
        /// Asynchronously updates this ESI key through a <see cref="ESIKeyCreationEventArgs"/>.
        /// </summary>
        /// <param name="accessResponse">The access and refresh token.</param>
        /// <param name="callback">A callback invoked on the UI thread (whatever the result, success or failure)</param>
        /// <returns></returns>
        public void TryUpdateAsync(AccessResponse accessResponse, EventHandler<ESIKeyCreationEventArgs> callback)
        {
            TryAddOrUpdateAsync(ID, accessResponse, callback);
        }

        /// <summary>
        /// Updates the ESI key.
        /// </summary>
        /// <param name="e">The <see cref="ESIKeyCreationEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.ArgumentNullException">e</exception>
        public void Update(ESIKeyCreationEventArgs e)
        {
            e.ThrowIfNull(nameof(e));

            // Detect scopes that were revoked during re-authentication
            var revokedScopes = _authorizedScopes
                .Where(oldScope => !e.AuthorizedScopes.Contains(oldScope))
                .ToList();

            RefreshToken = e.RefreshToken;
            _authorizedScopes = new List<string>(e.AuthorizedScopes);
            // Throw out old access token
            AccessToken = string.Empty;
            m_keyExpires = DateTime.MinValue;
            CheckAccessToken();

            // Clear the ESI key for the currently associated identities
            foreach (CharacterIdentity id in AppServices.CharacterIdentities.Where(id => id.ESIKeys.Contains(this)))
                id.ESIKeys.Remove(this);

            // Assign this API key to the new identities and create CCP characters
            var cid = e.Identity;
            cid.ESIKeys.Add(this);

            // Clear stale data for any scopes that were revoked
            if (revokedScopes.Count > 0 && cid.CCPCharacter != null)
                cid.CCPCharacter.ClearRevokedScopeData(revokedScopes);

            // Retrieves the ccp character and create one if none
            if (cid.CCPCharacter != null)
            {
                // Notify subscribers
                AppServices.TraceService?.Trace(cid.CCPCharacter.Name);
                AppServices.EventAggregator?.Publish(new CoreEvents.CharacterUpdatedEvent(cid.CCPCharacter.CharacterID, cid.CCPCharacter.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpdatedEvent(cid.CCPCharacter));
            }
            else
                AppServices.Characters.Add(AppServices.CharacterFactory.CreateCCPCharacter(cid));
        }

        #endregion


        #region Overridden Methods

        public override bool Equals(object obj)
        {
            var other = obj as ESIKey;
            return other != null && other.ID == ID;
        }

        public override int GetHashCode()
        {
            return (int)(ID & 0x7FFFFFFFL);
        }

        /// <summary>
        /// Gets a string representation of this API key, under the given format : 123456 (John Doe, Jane Doe).
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // If no characters on this API key, return only the API key ID
            if (!CharacterIdentities.Any())
                return ID.ToString(CultureConstants.DefaultCulture);

            // Otherwise, return the chars' names into parenthesis
            StringBuilder names = new StringBuilder();
            foreach (CharacterIdentity id in CharacterIdentities)
            {
                names.Append(id.CharacterName);
                if (id != CharacterIdentities.Last())
                    names.Append(", ");
            }
            return $"{ID} ({names})";
        }

        #endregion


        #region Helper Class


        #region Nested type: ResponseState

        private enum ResponseState
        {
            Unknown,
            InError,
            Training,
            NotTraining
        }

        #endregion


        #region Nested type: SkillInTrainingResponse

        private class SkillInTrainingResponse
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SkillInTrainingResponse"/> class.
            /// </summary>
            public SkillInTrainingResponse()
            {
                State = ResponseState.Unknown;
            }

            /// <summary>
            /// Gets or sets the state.
            /// </summary>
            /// <value>The state.</value>
            public ResponseState State { get; set; }
        }

        #endregion

        #endregion

    }
}
