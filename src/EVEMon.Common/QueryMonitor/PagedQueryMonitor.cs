// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVEMon.Common.Extensions;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Services;

namespace EVEMon.Common.QueryMonitor
{
    /// <summary>
    /// Represents a monitor for all queries related to characters and their corporations,
    /// supporting paged requests.
    /// </summary>
    /// <typeparam name="T">The outer container type.</typeparam>
    /// <typeparam name="U">The inner result type.</typeparam>
    public sealed class PagedQueryMonitor<T, U> : QueryMonitor<T> where T : List<U>
        where U : class
    {
        private readonly CCPQueryMonitorBase<T> wrapped;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="wrapped">The query monitor to wrap.</param>
        internal PagedQueryMonitor(CCPQueryMonitorBase<T> wrapped) : base(wrapped.Method,
            wrapped.Callback, suppressSelfTicking: true)
        {
            this.wrapped = wrapped;

            // The inner wrapped monitor also subscribed to FiveSecondTick in its own
            // QueryMonitor constructor. Since only the outer PagedQueryMonitor should be
            // driven (by its parent or self-ticking), suppress the inner to avoid phantom
            // no-op handler invocations (significant at 100+ characters).
            ((IQueryMonitorEx)wrapped).SuppressSelfTicking();
        }

        /// <summary>
        /// Gets the required API key information are known.
        /// </summary>
        /// <returns>False if an API key was required and not found.</returns>
        internal override bool HasESIKey => wrapped.HasESIKey;

        /// <summary>
        /// Gets a value indicating whether this monitor has access to data.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this monitor has access; otherwise, <c>false</c>.
        /// </value>
        public override bool HasAccess => wrapped.HasAccess;

        /// <summary>
        /// Performs the paged query to the provider asynchronously using modern async/await pattern.
        /// </summary>
        /// <param name="provider">The API provider to use.</param>
        protected override async Task QueryAsyncCoreAsync(APIProvider provider)
        {
            provider.ThrowIfNull(nameof(provider));

            try
            {
                var result = await provider.QueryPagedEsiAsync<T, U>(Method, wrapped.GetESIParams())
                    .ConfigureAwait(false);

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
