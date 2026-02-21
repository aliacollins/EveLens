// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Constants;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Net;
using EVEMon.Common.Serialization;
using EVEMon.Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common.Service
{
    public static class EveFlag
    {
        private static Dictionary<int, SerializableEveFlagsListItem> s_eveFlags =
            new Dictionary<int, SerializableEveFlagsListItem>();
        private static bool s_isLoaded;
        private static bool s_queryPending;
        private static DateTime s_nextCheckTime;

        public const string Filename = "Flags";

        /// <summary>
        /// Gets the description of the flag.
        /// </summary>
        /// <param name="id">The flag id.</param>
        public static string GetFlagText(int id)
        {
            if (EveMonClient.IsDebugBuild)
                EnsureInitialized();
            else
                EnsureImportation();

            SerializableEveFlagsListItem flag = null;
            // Some flags have been introduced that are not in the SDE
            if (s_eveFlags != null && !s_eveFlags.TryGetValue(id, out flag))
                flag = null;

            return flag?.Text ?? EveMonConstants.UnknownText;
        }

        /// <summary>
        /// Gets the description of the flag.
        /// </summary>
        /// <param name="name">The flag name.</param>
        public static int GetFlagID(string name)
        {
            if (EveMonClient.IsDebugBuild)
                EnsureInitialized();
            else
                EnsureImportation();

            SerializableEveFlagsListItem flag = null;
            if (s_eveFlags != null)
                flag = s_eveFlags.Values.FirstOrDefault(x => x.Name.Equals(name,
                    StringComparison.InvariantCultureIgnoreCase));
            
            return flag?.ID ?? 0;
        }

        /// <summary>
        /// Ensures the eve flags data have been intialized.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (s_isLoaded)
                return;

            SerializableEveFlags result = Util.DeserializeXmlFromString<SerializableEveFlags>(
                Properties.Resources.Flags, APIProvider.RowsetsTransform);

            Import(result);
        }

        /// <summary>
        /// Ensures the importation.
        /// </summary>
        private static void EnsureImportation()
        {
            // Quit if we already checked a minute ago or query is pending
            if (s_nextCheckTime > DateTime.UtcNow || s_queryPending || s_isLoaded)
                return;

            s_nextCheckTime = DateTime.UtcNow.AddMinutes(1);

            // Deserialize the xml file
            var result = LocalXmlCache.Load<SerializableEveFlags>(Filename, true);
            if (result == null)
            {
                Task.WhenAll(UpdateFileAsync());
                s_nextCheckTime = DateTime.UtcNow;
            }
            else
                // Import the data
                Import(result);
        }

        /// <summary>
        /// Imports the specified result.
        /// </summary>
        /// <param name="result">The result.</param>
        private static void Import(SerializableEveFlags result)
        {
            if (result == null)
            {
                AppServices.TraceService?.Trace("failed");
                return;
            }

            AppServices.TraceService?.Trace("begin");

            s_eveFlags.Clear();
            // This is way faster to look up flags
            foreach (var flag in result.EVEFlags)
                s_eveFlags.Add(flag.ID, flag);

            s_isLoaded = true;

            AppServices.TraceService?.Trace("done");
        }

        /// <summary>
        /// Updates the file.
        /// </summary>
        private static async Task UpdateFileAsync()
        {
            // Quit if query is pending
            if (s_queryPending)
                return;

            var url = new Uri(NetworkConstants.BitBucketWikiBase + NetworkConstants.EveFlags);

            s_queryPending = true;

            var result = await Util.DownloadXmlAsync<SerializableEveFlags>(url,
                new RequestParams()
                {
                    AcceptEncoded = true
                }, APIProvider.RowsetsTransform);
            OnDownloaded(result);
        }

        /// <summary>
        /// Processes the queried eve flags.
        /// </summary>
        /// <param name="result">The result.</param>
        private static void OnDownloaded(DownloadResult<SerializableEveFlags> result)
        {
            if (result.Error != null)
            {
                // Reset query pending flag
                s_queryPending = false;

                AppServices.TraceService?.Trace(result.Error.Message);

                // Fallback
                EnsureInitialized();
                return;
            }

            // Import the list
            Import(result.Result);

            // Reset query pending flag
            s_queryPending = false;

            // Notify the subscribers
            AppServices.TraceService?.Trace("EveFlagsUpdated");
            AppServices.EventAggregator?.Publish(CommonEvents.EveFlagsUpdatedEvent.Instance);

            // Save the file in cache
            LocalXmlCache.SaveAsync(Filename, Util.SerializeToXmlDocument(result.Result)).
                ConfigureAwait(false);
        }
    }
}
