// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Models;

namespace EveLens.Common.QueryMonitor
{
    /// <summary>
    /// Represents a monitor for all queries related to corporations.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    public sealed class CorporationQueryMonitor<T> : CCPQueryMonitorBase<T> where T : class
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character">The character to monitor.</param>
        /// <param name="method">The method to use.</param>
        /// <param name="onSuccess">An action to call on success.</param>
        /// <param name="onFailure">The callback to use upon failure.</param>
        internal CorporationQueryMonitor(CCPCharacter character, Enum method, Action<T>
            onSuccess, NotifyErrorCallback onFailure, bool suppressSelfTicking = false) : base(character, method, (result) =>
            {
                if (character.Monitored)
                {
                    // "No corp role(s)" = 403
                    if (result.HasError)
                    {
                        // Do not invoke onFailure on corp roles error since we cannot actually
                        // determine whether the key had the roles until we try
                        if (result.ErrorCode != 403 && character.ShouldNotifyError(result,
                                method))
                            onFailure.Invoke(character, result);
                    }
                    else if (result.HasData)
                        onSuccess.Invoke(result.Result);
                }
            }, suppressSelfTicking)
        {
        }

        /// <summary>
        /// Retrieves the parameters required for the ESI request.
        /// </summary>
        /// <returns>The ESI request parameters.</returns>
        internal override ESIParams GetESIParams()
        {
            // Ensure m_apiKey is set (may not be if HasAccess wasn't called recently)
            if (m_apiKey == null)
                m_apiKey = m_character.Identity.FindAPIKeyWithAccess((ESIAPICorporationMethods)Method);

            return new ESIParams(LastResult?.Response, m_apiKey?.AccessToken)
            {
                ParamOne = m_character.CorporationID
            };
        }

        /// <summary>
        /// Gets a value indicating whether this monitor has access to data.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this monitor has access; otherwise, <c>false</c>.
        /// </value>
        public override bool HasAccess
        {
            get
            {
                m_apiKey = m_character.Identity.FindAPIKeyWithAccess((ESIAPICorporationMethods)
                    Method);
                return !m_character.IsInNPCCorporation && m_apiKey != null;
            }
        }
    }
}
