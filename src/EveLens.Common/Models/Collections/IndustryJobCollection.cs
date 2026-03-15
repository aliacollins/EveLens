// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Collections;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Extensions;
using EveLens.Common.Interfaces;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;
using CommonEvents = EveLens.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EveLens.Common.Models.Collections
{
    /// <summary>
    /// A collection of industry jobs.
    /// </summary>
    public sealed class IndustryJobCollection : ReadonlyCollection<IndustryJob>
    {
        private readonly CCPCharacter m_ccpCharacter;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        internal IndustryJobCollection(CCPCharacter character)
        {
            m_ccpCharacter = character;
            m_ccpCharacter.Services.FiveSecondTick += EveLensClient_TimerTick;
        }

        /// <summary>
        /// Called when the object gets disposed.
        /// </summary>
        internal void Dispose()
        {
            m_ccpCharacter.Services.FiveSecondTick -= EveLensClient_TimerTick;
        }

        /// <summary>
        /// Handles the TimerTick event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveLensClient_TimerTick(object sender, EventArgs e)
        {
            IQueryMonitor charIndustryJobsMonitor = m_ccpCharacter.QueryMonitors.Any(x =>
                (ESIAPICharacterMethods)x.Method == ESIAPICharacterMethods.IndustryJobs) ?
                m_ccpCharacter.QueryMonitors[ESIAPICharacterMethods.IndustryJobs] : null;
            IQueryMonitor corpIndustryJobsMonitor = m_ccpCharacter.QueryMonitors.Any(x =>
                (ESIAPICorporationMethods)x.Method == ESIAPICorporationMethods.
                CorporationIndustryJobs) ? m_ccpCharacter.QueryMonitors[
                ESIAPICorporationMethods.CorporationIndustryJobs] : null;

            if ((charIndustryJobsMonitor != null && charIndustryJobsMonitor.Enabled) ||
                (corpIndustryJobsMonitor != null && corpIndustryJobsMonitor.Enabled))
                UpdateOnTimerTick();
        }

        /// <summary>
        /// Imports an enumeration of serialization objects.
        /// </summary>
        /// <param name="src"></param>
        internal void Import(IEnumerable<SerializableJob> src)
        {
            Items.Clear();
            foreach (SerializableJob srcJob in src)
                Items.Add(new IndustryJob(srcJob) { InstallerID = m_ccpCharacter.CharacterID });
        }

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable jobs from the API.</param>
        /// <param name="issuedFor">Whether these jobs were issued for the corporation or
        /// character.</param>
        internal void Import(IEnumerable<EsiJobListItem> src, IssuedFor issuedFor)
        {
            // Mark all jobs for deletion, jobs found in the API will be unmarked
            foreach (IndustryJob job in Items)
                job.MarkedForDeletion = true;
            var newJobs = new LinkedList<IndustryJob>();
            var now = DateTime.UtcNow;
            // Import the jobs from the API
            foreach (EsiJobListItem job in src)
            {
                DateTime limit = job.EndDate.SafeAddDays(IndustryJob.MaxEndedDays);
                // For jobs which are not yet ended, or are active and not ready (active is
                // defined as having an empty completion date)
                if (limit >= now || (job.CompletedDate == DateTime.MinValue && job.Status !=
                    CCPJobCompletedStatus.Ready))
                {
                    // Where the job isn't already in the list
                    if (!Items.Any(x => x.TryImport(job, issuedFor, m_ccpCharacter)))
                    {
                        // Only add jobs with valid items
                        var ij = new IndustryJob(job, issuedFor);
                        if (ij.InstalledItem != null && ij.OutputItem != null)
                            newJobs.AddLast(ij);
                    }
                }
            }
            // Add the items that are no longer marked for deletion
            newJobs.AddRange(Items.Where(x => !x.MarkedForDeletion));
            // Replace the old list with the new one
            Items.Clear();
            Items.AddRange(newJobs);
        }

        /// <summary>
        /// Exports only the character issued jobs to a serialization object for the settings file.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Used to export only the corporation jobs issued by the character.</remarks>
        internal IEnumerable<SerializableJob> ExportOnlyIssuedByCharacter() => Items.Where(
            job => job.InstallerID == m_ccpCharacter.CharacterID).Select(job => job.Export());

        /// <summary>
        /// Exports the jobs to a serialization object for the settings file.
        /// </summary>
        /// <returns>List of serializable jobs.</returns>
        /// <remarks>Used to export all jobs of the collection.</remarks>
        internal IEnumerable<SerializableJob> Export() => Items.Select(job => job.Export());

        /// <summary>
        /// Notify the user on a job completion.
        /// </summary>
        private void UpdateOnTimerTick()
        {
            bool isCorporateMonitor = true;
            if (Items.Count > 0)
            {
                // Add the not notified "Ready" jobs to the completed list
                var jobsCompleted = new LinkedList<IndustryJob>();
                var characterJobs = new LinkedList<IndustryJob>();
                foreach (IndustryJob job in Items)
                {
                    if (job.IsActive && job.TTC.Length == 0 && !job.NotificationSend)
                    {
                        job.NotificationSend = true;
                        jobsCompleted.AddLast(job);
                        // Track if "jobs on behalf of character" needs to also be displayed
                        if (job.InstallerID == m_ccpCharacter.CharacterID)
                            characterJobs.AddLast(job);
                    }
                    // If this job was not issued for corp, ensure notification is for that
                    // character only
                    if (job.IssuedFor != IssuedFor.Corporation)
                        isCorporateMonitor = false;
                }
                // Only notify if jobs have been completed
                if (jobsCompleted.Count > 0)
                {
                    // Sends a notification
                    if (isCorporateMonitor)
                    {
                        if (characterJobs.Count > 0)
                        {
                            // Fire event for corporation job completion on behalf of character
                            AppServices.TraceService?.Trace(m_ccpCharacter.Name);
                            m_ccpCharacter.OnCharacterIndustryJobsCompleted(characterJobs);
                            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterIndustryJobsCompletedEvent(m_ccpCharacter, characterJobs));
                        }
                        // Fire event for corporation job completion
                        AppServices.TraceService?.Trace(m_ccpCharacter.CorporationName);
                        m_ccpCharacter.OnCorporationIndustryJobsCompleted(jobsCompleted);
                        AppServices.EventAggregator?.Publish(new CommonEvents.CorporationIndustryJobsCompletedEvent(m_ccpCharacter, jobsCompleted));
                    }
                    else
                    {
                        // Fire event for character job completion
                        AppServices.TraceService?.Trace(m_ccpCharacter.Name);
                        m_ccpCharacter.OnCharacterIndustryJobsCompleted(jobsCompleted);
                        AppServices.EventAggregator?.Publish(new CommonEvents.CharacterIndustryJobsCompletedEvent(m_ccpCharacter, jobsCompleted));
                    }
                }
            }
        }
    }
}
