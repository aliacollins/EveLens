// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EveLens.Common.Attributes;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Extensions;
using EveLens.Common.Interfaces;
using EveLens.Common.Models;
using EveLens.Common.Net;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Core.Events;

namespace EveLens.Common.QueryMonitor
{
    /// <summary>
    /// This class monitors a querying process. It provides services for autoupdating, update
    /// notification, and querying character data.
    /// </summary>
    [EnforceUIThreadAffinity]
    public class QueryMonitor<T> : IQueryMonitorEx, INetworkChangeSubscriber where T : class
    {
        // Matches the error reporting methods in GlobalNotificationCollection
        internal delegate void NotifyErrorCallback(CCPCharacter character, EsiResult<T> result);

        private readonly bool m_selfTicking;
        private IDisposable? m_fiveSecondTickSubscription;
        private bool m_forceUpdate;
        private bool m_isCanceled;
        private bool m_retryOnForceUpdateError;


        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="callback">The callback.</param>
        /// <exception cref="System.ArgumentNullException">callback;@The callback cannot be null.</exception>
        internal QueryMonitor(Enum method, Action<EsiResult<T>> callback, bool suppressSelfTicking = false)
        {
            // Check callback not null
            callback.ThrowIfNull(nameof(callback), "The callback cannot be null.");

            LastUpdate = DateTime.MinValue;
            m_forceUpdate = true;
            Callback = callback;
            Method = method;
            Enabled = false;
            QueryOnStartup = false;
            m_selfTicking = !suppressSelfTicking;

            NetworkMonitor.Register(this);

            // Use FiveSecondTick - API cache expiry is typically minutes/hours
            if (m_selfTicking)
                m_fiveSecondTickSubscription = AppServices.EventAggregator?.Subscribe<FiveSecondTickEvent>(
                    e => EveLensClient_TimerTick(null, EventArgs.Empty));
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets true if the query is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets true whether the monitor has to do a query on application startup.
        /// </summary>
        public bool QueryOnStartup { get; set; }

        /// <summary>
        /// Gets the API method monitored by this instance.
        /// </summary>
        public Enum Method { get; }

		/// <summary>
		/// Gets the callback used for this query monitor.
		/// </summary>
		internal Action<EsiResult<T>> Callback { get; }

		/// <summary>
		/// Gets the last time this instance was updated (UTC).
		/// </summary>
		public DateTime LastUpdate { get; private set; }

        /// <summary>
        /// Gets the status of the query.
        /// </summary>
        public QueryStatus Status { get; private set; }

        /// <summary>
        /// Gets true when the API provider is not CCP or the cache timer has expired.
        /// </summary>
        public bool CanForceUpdate
        {
            get
            {
                if (AppServices.APIProviders.CurrentProvider != APIProvider.DefaultProvider &&
                    AppServices.APIProviders.CurrentProvider != APIProvider.TestProvider)
                    return true;

                return DateTime.UtcNow > (LastResult?.CachedUntil ?? NextUpdate);
            }
        }

        /// <summary>
        /// Gets the next time this instance should be updated (UTC), based on both the CCP cache time and the user preferences.
        /// </summary>
        public DateTime NextUpdate
        {
            get
            {
                DateTime nextUpdate;
                // If there was an error on last try, we use the cached time
                if (LastResult != null && LastResult.HasError)
                    return LastResult.CachedUntil;

                // If EsiScheduler provided a CachedUntil override, use it
                if (m_cachedUntilOverride.HasValue && m_cachedUntilOverride.Value > DateTime.UtcNow)
                    return m_cachedUntilOverride.Value;

                // No error ? Then we compute the next update according to the settings
                var period = UpdatePeriod.Never;
                string method = Method.ToString();
                if (Settings.Updates.Periods.ContainsKey(method))
                    period = Settings.Updates.Periods[method];
                if (period == UpdatePeriod.Never)
                    nextUpdate = DateTime.MaxValue;
                else
                {
                    nextUpdate = LastUpdate.Add(period.ToDuration());
                    // If CCP "cached until" is greater than what we computed, return CCP cached time
                    if (LastResult != null && LastResult.CachedUntil > nextUpdate)
                        return LastResult.CachedUntil;

                }
                return nextUpdate;
            }
        }

        /// <summary>
        /// CachedUntil override set by EsiScheduler for NextUpdate computation.
        /// </summary>
        private DateTime? m_cachedUntilOverride;

        /// <summary>
        /// Gets the parameters from the last ESI response.
        /// </summary>
        public EsiResult<T> LastResult { get; private set; }

        /// <summary>
        /// Gets true whether the method is curently being requeried.
        /// </summary>
        public bool IsUpdating { get; private set; }

        /// <summary>
        /// Gets true when the monitor encountered an error on last try.
        /// </summary>
        public bool HasError => LastResult != null && LastResult.HasError;

        /// <summary>
        /// Gets true if this monitor has access to data.
        /// </summary>
        public virtual bool HasAccess => true;

        /// <summary>
        /// Gets the required API key information are known.
        /// </summary>
        /// <returns>False if an API key was required and not found.</returns>
        internal virtual bool HasESIKey => true;

        /// <summary>
        /// Gets whether this is the ServerStatus query (which should always run).
        /// </summary>
        private bool IsServerStatusQuery => Method is ESIAPIGenericMethods.ServerStatus;

        #endregion


        #region  Event Handlers


        /// <summary>
        /// Handles the TimerTick event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveLensClient_TimerTick(object sender, EventArgs e)
        {
            UpdateOnOneSecondTick();
        }

        #endregion


        /// <summary>
        /// Called when the object gets disposed.
        /// </summary>
        public void Dispose()
        {
            m_fiveSecondTickSubscription?.Dispose();
            m_fiveSecondTickSubscription = null;
        }

        /// <summary>
        /// Manually updates this monitor with the provided data, like if it has just been
        /// updated from CCP.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <remarks>
        /// This method does not fire any event.
        /// </remarks>
        internal void UpdateWith(EsiResult<T> result)
        {
            LastResult = result;
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// Forces an update.
        /// </summary>
        internal void ForceUpdate(bool retryOnError)
        {
            m_forceUpdate = true;
            m_retryOnForceUpdateError |= retryOnError;
        }

        /// <summary>
        /// Updates display status on every tick. Display-only — no HTTP fetching.
        /// All HTTP fetching is handled exclusively by EsiScheduler via the orchestrator's
        /// fetch closures. Monitors are pure display objects (Status, LastUpdate, IsUpdating)
        /// for the UI throbber. SetExternalStatus() is the bridge from scheduler to monitor.
        /// </summary>
        private void UpdateOnOneSecondTick()
        {
            // Only update display status — never initiate HTTP requests.
            // EsiScheduler is the sole fetcher; it calls SetExternalStatus() to
            // update IsUpdating/Status/LastUpdate for the UI throbber.
            if (!IsUpdating)
            {
                if (!Enabled)
                    Status = QueryStatus.Disabled;
                else if (!NetworkMonitor.IsNetworkAvailable)
                    Status = QueryStatus.NoNetwork;
                else if (!IsServerStatusQuery && !AppServices.EVEServer.IsOnline)
                    Status = QueryStatus.ServerOffline;
                else if (!HasESIKey)
                    Status = QueryStatus.NoESIKey;
                else
                    Status = QueryStatus.Pending;
            }
        }

        /// <summary>
        /// Performs the query to the provider asynchronously using modern async/await pattern.
        /// </summary>
        /// <param name="provider">The API provider to use.</param>
        protected virtual async Task QueryAsyncCoreAsync(APIProvider provider)
        {
            provider.ThrowIfNull(nameof(provider));

            try
            {
                var result = await provider.QueryEsiAsync<T>(Method, new ESIParams(LastResult?.Response))
                    .ConfigureAwait(false);

                // Marshal back to UI thread and invoke callback
                AppServices.Dispatcher?.Invoke(() => OnQueried(result));
            }
            catch (Exception ex)
            {
                // Ensure IsUpdating is reset even if an exception occurs
                // Also log the exception for debugging
                AppServices.Dispatcher?.Invoke(() => ResetUpdatingState(ex));
            }
        }

        /// <summary>
        /// Occurs when a new result has been queried.
        /// </summary>
        /// <param name="result">The downloaded result</param>
        protected void OnQueried(EsiResult<T> result)
        {
            IsUpdating = false;
            Status = QueryStatus.Pending;

            // Do we need to retry the force update ?
            m_forceUpdate = m_retryOnForceUpdateError && result.HasError;

            if (!m_isCanceled)
            {
                // Updates the stored data
                m_retryOnForceUpdateError = false;
                LastUpdate = DateTime.UtcNow;
                LastResult = result;
                // Notify subscribers
                Callback?.Invoke(result);

                // Debug: Log completion
                AppServices.TraceService?.Trace($"QueryMonitor.OnQueried - {Method} completed, HasError={result.HasError}, NextUpdate={NextUpdate:HH:mm:ss}");
            }
            else
            {
                AppServices.TraceService?.Trace($"QueryMonitor.OnQueried - {Method} was canceled");
            }
        }

        /// <summary>
        /// Resets the updating state when an exception occurs during async query.
        /// Also sets LastUpdate to prevent immediate retry (backoff).
        /// </summary>
        /// <param name="ex">The exception that occurred, if any.</param>
        protected void ResetUpdatingState(Exception ex = null)
        {
            var exMessage = ex?.GetBaseException().Message ?? "unknown";
            AppServices.TraceService?.Trace($"QueryMonitor.ResetUpdatingState - {Method} exception: {exMessage}");
            IsUpdating = false;
            Status = QueryStatus.Pending;
            // Set LastUpdate to current time to prevent immediate retry loop
            // The query will retry after the normal update period
            LastUpdate = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates monitor status externally when the fetch is driven by EsiScheduler
        /// rather than the monitor's own HTTP path. Used for UI status display (throbber).
        /// </summary>
        void IQueryMonitorEx.SetExternalStatus(bool isUpdating, DateTime? lastUpdate)
        {
            IsUpdating = isUpdating;
            Enabled = true;
            if (isUpdating)
            {
                Status = QueryStatus.Updating;
            }
            else
            {
                Status = QueryStatus.Pending;
                if (lastUpdate.HasValue)
                    LastUpdate = lastUpdate.Value;
            }
        }

        /// <summary>
        /// Sets the CachedUntil override for NextUpdate computation.
        /// Called by EsiScheduler closures after a fetch completes with cache metadata.
        /// </summary>
        void IQueryMonitorEx.SetCachedUntilOverride(DateTime cachedUntil)
        {
            m_cachedUntilOverride = cachedUntil;
        }

        /// <summary>
        /// Resets the monitor with the given last update time.
        /// </summary>
        /// <param name="lastUpdate">The UTC time of the last update.</param>
        private void Reset(DateTime lastUpdate)
        {
            // Cancel any running request, but preserve m_forceUpdate for startup queries
            m_isCanceled = true;
            LastUpdate = lastUpdate;
            LastResult = null;
            // If QueryOnStartup is true, ensure first query runs regardless of cached time
            // This fixes the bug where assets weren't fetched on restart because
            // the cached time was restored but the actual data was not persisted
            if (QueryOnStartup)
                m_forceUpdate = true;
        }

        /// <summary>
        /// Cancels the running update.
        /// </summary>
        private void Cancel()
        {
            m_isCanceled = true;
            m_forceUpdate = false;
        }

        /// <summary>
        /// Set the network availability.
        /// </summary>
        protected bool SetNetworkStatus { get; set; }


        #region Overridden Methods

        /// <summary>
        /// Gets the bound method header.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Method.HasHeader() ? Method.GetHeader() : Method.
            ToString();

        #endregion


        #region Interfaces implementations

        bool INetworkChangeSubscriber.SetNetworkStatus
        {
            get { return SetNetworkStatus; }
            set { SetNetworkStatus = value; }
        }

        void IQueryMonitorEx.Reset(DateTime lastUpdate)
        {
            Reset(lastUpdate);
        }

        void IQueryMonitorEx.ForceUpdate(bool retryOnError)
        {
            ForceUpdate(retryOnError);
        }

        void IQueryMonitorEx.UpdateTick()
        {
            UpdateOnOneSecondTick();
        }

        void IQueryMonitorEx.SuppressSelfTicking()
        {
            m_fiveSecondTickSubscription?.Dispose();
            m_fiveSecondTickSubscription = null;
        }

        IAPIResult IQueryMonitor.LastResult => LastResult;

        #endregion

    }
}
