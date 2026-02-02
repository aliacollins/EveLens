using EVEMon.Common.Data;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization;
using EVEMon.Common.Serialization.Datafiles;
using EVEMon.Common.Serialization.Eve;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EVEMon.Common.Service
{
    /// <summary>
    /// Converts citadel and NPC station IDs to station information.
    /// Uses a centralized lookup service for citadels to handle request deduplication
    /// and character rotation efficiently.
    /// </summary>
    public static class EveIDToStation
    {
        private const string Filename = "ConquerableStationList";

        /// <summary>
        /// Centralized lookup service for structures.
        /// </summary>
        private static readonly StructureLookupService s_lookupService = new StructureLookupService();

        private static bool s_savePending;
        private static DateTime s_lastSaveTime;

        /// <summary>
        /// Static Constructor.
        /// </summary>
        static EveIDToStation()
        {
            // Use ThirtySecondTick - batch station resolution is a background task
            EveMonClient.ThirtySecondTick += EveMonClient_TimerTick;
            s_lookupService.DataChanged += OnLookupServiceDataChanged;
        }

        /// <summary>
        /// Handles data changed events from the lookup service.
        /// </summary>
        private static void OnLookupServiceDataChanged(object sender, EventArgs e)
        {
            EveMonClient.OnConquerableStationListUpdated();
            s_savePending = true;
        }

        /// <summary>
        /// Handles the TimerTick event for periodic saves.
        /// </summary>
        private static async void EveMonClient_TimerTick(object sender, EventArgs e)
        {
            await UpdateOnOneSecondTickAsync();
        }

        /// <summary>
        /// Gets the station information from its ID. Works on NPC stations and citadels.
        /// For citadels, this initiates an async lookup if not cached.
        /// </summary>
        /// <param name="id">The station/structure ID.</param>
        /// <param name="character">Optional character context (helps prioritize ESI key selection).</param>
        /// <returns>The station information, or an inaccessible placeholder if lookup is pending.</returns>
        internal static Station GetIDToStation(long id, CCPCharacter character = null)
        {
            // Check NPC stations first (these are in static data)
            var station = StaticGeography.GetStationByID(id);
            if (station != null)
                return station;

            // Citadels have ID over maximum int value
            if (id > int.MaxValue)
            {
                var serStation = s_lookupService.LookupStructure(id, character);
                if (serStation != null)
                    return new Station(serStation);

                // Return inaccessible placeholder while lookup is in progress
                return Station.CreateInaccessible(id);
            }

            return null;
        }

        /// <summary>
        /// Async version of GetIDToStation that waits for lookup to complete.
        /// Use this when you need to ensure the station info is available.
        /// </summary>
        /// <param name="id">The station/structure ID.</param>
        /// <param name="character">Optional character context.</param>
        /// <returns>The station information, or an inaccessible placeholder if not accessible.</returns>
        internal static async Task<Station> GetIDToStationAsync(long id, CCPCharacter character = null)
        {
            // Check NPC stations first
            var station = StaticGeography.GetStationByID(id);
            if (station != null)
                return station;

            // Citadels
            if (id > int.MaxValue)
            {
                var serStation = await s_lookupService.LookupStructureAsync(id, character)
                    .ConfigureAwait(false);

                if (serStation != null)
                    return new Station(serStation);

                return Station.CreateInaccessible(id);
            }

            return null;
        }

        /// <summary>
        /// Initializes the cache from file.
        /// </summary>
        public static void InitializeFromFile()
        {
            if (EveMonClient.Closed)
                return;

            // Only load if we haven't already
            if (s_lookupService.CacheCount > 0)
                return;

            var cache = LocalXmlCache.Load<SerializableStationList>(Filename, true);
            if (cache != null)
                s_lookupService.ImportFromCache(cache.Stations);
        }

        /// <summary>
        /// Periodic save check.
        /// </summary>
        private static Task UpdateOnOneSecondTickAsync()
        {
            if (s_savePending && DateTime.UtcNow > s_lastSaveTime.AddSeconds(10))
                return SaveImmediateAsync();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Saves the cache to file immediately.
        /// </summary>
        public static async Task SaveImmediateAsync()
        {
            var serial = new SerializableStationList();
            foreach (var station in s_lookupService.ExportCache())
                serial.Stations.Add(station);

            await LocalXmlCache.SaveAsync(Filename, Util.SerializeToXmlDocument(serial));

            s_lastSaveTime = DateTime.UtcNow;
            s_savePending = false;
        }
    }
}
