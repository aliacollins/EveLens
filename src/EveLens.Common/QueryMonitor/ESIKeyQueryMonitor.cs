// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Extensions;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Services;

namespace EveLens.Common.QueryMonitor
{
    public sealed class ESIKeyQueryMonitor<T> : QueryMonitor<T> where T : class
    {
        private readonly ESIKey m_esiKey;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="method"></param>
        /// <param name="onUpdated"></param>
        internal ESIKeyQueryMonitor(ESIKey apiKey, Enum method, Action<EsiResult<T>> onUpdated)
            : base(method, onUpdated)
        {
            m_esiKey = apiKey;
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
                if (Method is ESIAPIGenericMethods)
                    return true;

                if (Method is ESIAPICharacterMethods charMethod)
                    return m_esiKey.HasAccessTo(charMethod);

                if (Method is ESIAPICorporationMethods corpMethod)
                    return m_esiKey.HasAccessTo(corpMethod);

                return true;
            }
        }

        /// <summary>
        /// Performs the query to the provider asynchronously using modern async/await pattern.
        /// </summary>
        /// <param name="provider">The API provider to use.</param>
        protected override async Task QueryAsyncCoreAsync(APIProvider provider)
        {
            provider.ThrowIfNull(nameof(provider));

            try
            {
                var result = await provider.QueryEsiAsync<T>(Method, new ESIParams(LastResult?.Response,
                    m_esiKey.AccessToken)).ConfigureAwait(false);

                // Marshal back to UI thread and call OnQueried for proper bookkeeping
                AppServices.Dispatcher?.Invoke(() => OnQueried(result));
            }
            catch (Exception ex)
            {
                // Ensure IsUpdating is reset even if an exception occurs
                AppServices.Dispatcher?.Invoke(() => ResetUpdatingState(ex));
            }
        }

    }
}
