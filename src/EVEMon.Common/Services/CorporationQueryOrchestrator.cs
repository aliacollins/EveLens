// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Net;
using EVEMon.Common.QueryMonitor;
using EVEMon.Common.Serialization.Esi;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Core.Events;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Production-grade corporation query orchestrator that replaces CorporationDataQuerying.
    /// Creates and drives all 4 corporation ESI query monitors for a character,
    /// handles all callbacks, and implements ICorporationDataQuerying.
    /// </summary>
    internal sealed class CorporationQueryOrchestrator : ICorporationDataQuerying
    {
        #region Fields

        private readonly QueryMonitor<EsiAPIMedals> m_corpMedalsMonitor;
        private readonly QueryMonitor<EsiAPIMarketOrders> m_corpMarketOrdersMonitor;
        private readonly QueryMonitor<EsiAPIContracts> m_corpContractsMonitor;
        private readonly QueryMonitor<EsiAPIIndustryJobs> m_corpIndustryJobsMonitor;
        private readonly List<IQueryMonitorEx> m_corporationQueryMonitors;
        private readonly CCPCharacter m_ccpCharacter;

        #endregion


        #region Constructor

        /// <summary>
        /// Production constructor — creates real ESI monitors and callbacks.
        /// Called from CCPCharacter when ESI key info is updated.
        /// </summary>
        internal CorporationQueryOrchestrator(CCPCharacter ccpCharacter)
        {
            m_ccpCharacter = ccpCharacter;
            m_corporationQueryMonitors = new List<IQueryMonitorEx>(4);

            // Initializes the query monitors.
            // Corp monitors self-tick via FiveSecondTickEvent (no suppressSelfTicking).
            // Corp data has slow ESI cache intervals (30min+) and doesn't need
            // EsiScheduler priority scheduling — self-ticking is sufficient.
            m_corpMedalsMonitor = new PagedQueryMonitor<EsiAPIMedals, EsiMedalsListItem>(
                new CorporationQueryMonitor<EsiAPIMedals>(ccpCharacter,
                ESIAPICorporationMethods.CorporationMedals, OnMedalsUpdated,
                AppServices.Notifications.NotifyCorporationMedalsError)
                { QueryOnStartup = true });
            // Add the monitors in an order as they will appear in the throbber menu
            m_corporationQueryMonitors.Add(m_corpMedalsMonitor);
            m_corpMarketOrdersMonitor = new PagedQueryMonitor<EsiAPIMarketOrders,
                EsiOrderListItem>(new CorporationQueryMonitor<EsiAPIMarketOrders>(ccpCharacter,
                ESIAPICorporationMethods.CorporationMarketOrders, OnMarketOrdersUpdated,
                AppServices.Notifications.NotifyCorporationMarketOrdersError)
                { QueryOnStartup = true });
            m_corporationQueryMonitors.Add(m_corpMarketOrdersMonitor);
            m_corpContractsMonitor = new PagedQueryMonitor<EsiAPIContracts,
                EsiContractListItem>(new CorporationQueryMonitor<EsiAPIContracts>(ccpCharacter,
                ESIAPICorporationMethods.CorporationContracts, OnContractsUpdated,
                AppServices.Notifications.NotifyCorporationContractsError)
                { QueryOnStartup = true });
            m_corporationQueryMonitors.Add(m_corpContractsMonitor);
            m_corpIndustryJobsMonitor = new PagedQueryMonitor<EsiAPIIndustryJobs,
                EsiJobListItem>(new CorporationQueryMonitor<EsiAPIIndustryJobs>(
                ccpCharacter, ESIAPICorporationMethods.CorporationIndustryJobs,
                OnIndustryJobsUpdated, AppServices.Notifications.
                NotifyCorporationIndustryJobsError) { QueryOnStartup = true });
            m_corporationQueryMonitors.Add(m_corpIndustryJobsMonitor);

            foreach (var monitor in m_corporationQueryMonitors)
                ccpCharacter.QueryMonitors.Add(monitor);
        }

        #endregion


        #region Properties

        /// <inheritdoc />
        public bool CorporationMarketOrdersQueried => !m_corpMarketOrdersMonitor.IsUpdating;

        /// <inheritdoc />
        public bool CorporationContractsQueried => !m_corpContractsMonitor.IsUpdating;

        /// <inheritdoc />
        public bool CorporationIndustryJobsQueried => !m_corpIndustryJobsMonitor.IsUpdating;

        #endregion


        #region ProcessTick

        /// <summary>
        /// Processes a single tick, driving all corporation query monitors.
        /// </summary>
        public void ProcessTick()
        {
            foreach (var monitor in m_corporationQueryMonitors)
                monitor.UpdateTick();
        }

        #endregion


        #region Dispose

        /// <summary>
        /// Cleans up resources and unregisters from scheduler.
        /// </summary>
        public void Dispose()
        {
            // Unsubscribe events in monitors
            foreach (IQueryMonitorEx monitor in m_corporationQueryMonitors)
            {
                monitor.Dispose();
            }
        }

        #endregion


        #region Callbacks

        /// <summary>
        /// Processes the queried character's corporation medals.
        /// </summary>
        private void OnMedalsUpdated(EsiAPIMedals result)
        {
            var target = m_ccpCharacter;

            // Character may have been deleted since we queried
            if (target != null)
            {
                target.CorporationMedals.Import(result, false);
                // CorporationMedalsUpdated
                AppServices.TraceService?.Trace($"CorporationMedalsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CommonEvents.CorporationMedalsUpdatedEvent(target));
            }
        }

        /// <summary>
        /// Processes the queried character's corporation market orders.
        /// </summary>
        private void OnMarketOrdersUpdated(EsiAPIMarketOrders result)
        {
            var target = m_ccpCharacter;

            // Character may have been deleted since we queried
            if (target != null)
            {
                var endedOrders = new LinkedList<MarketOrder>();
                target.CorporationMarketOrders.Import(result, IssuedFor.Corporation,
                    endedOrders);
                // CorporationMarketOrdersUpdated
                AppServices.TraceService?.Trace($"CorporationMarketOrdersUpdated: {target.CorporationName}");
                (target as CCPCharacter)?.OnCorporationMarketOrdersUpdated(endedOrders);
                AppServices.EventAggregator?.Publish(new CommonEvents.CorporationMarketOrdersEndedEvent(target, endedOrders));
            }
        }

        /// <summary>
        /// Processes the queried character's corporation contracts.
        /// </summary>
        private void OnContractsUpdated(EsiAPIContracts result)
        {
            var target = m_ccpCharacter;

            // Character may have been deleted since we queried
            if (target != null)
            {
                // Mark all contracts as corporation issued
                foreach (var contract in result)
                    contract.APIMethod = ESIAPICorporationMethods.CorporationContracts;
                var endedContracts = new List<Contract>();
                target.CorporationContracts.Import(result, endedContracts);
                // CorporationContractsUpdated
                AppServices.TraceService?.Trace($"CorporationContractsUpdated: {target.CorporationName}");
                (target as CCPCharacter)?.OnCorporationContractsUpdated(endedContracts);
                AppServices.EventAggregator?.Publish(new CommonEvents.CorporationContractsEndedEvent(target, endedContracts));
            }
        }

        /// <summary>
        /// Processes the queried character's corporation industry jobs.
        /// </summary>
        private void OnIndustryJobsUpdated(EsiAPIIndustryJobs result)
        {
            var target = m_ccpCharacter;

            // Character may have been deleted since we queried
            if (target != null)
            {
                // Mark all jobs as corporation issued
                target.CorporationIndustryJobs.Import(result, IssuedFor.Corporation);
                // CorporationIndustryJobsUpdated
                AppServices.TraceService?.Trace($"CorporationIndustryJobsUpdated: {target.Name}");
                (target as CCPCharacter)?.OnCorporationIndustryJobsUpdated();
                AppServices.EventAggregator?.Publish(new CommonEvents.CorporationIndustryJobsUpdatedEvent(target));
            }
        }

        #endregion
    }
}
