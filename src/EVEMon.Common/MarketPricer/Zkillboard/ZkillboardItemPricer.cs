// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Collections;
using EVEMon.Common.Net;
using EVEMon.Common.Serialization.EveMarketer.MarketPricer;
using EVEMon.Common.Service;
using EVEMon.Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonEvents = EVEMon.Common.Events;

using ZkillPriceHistory = System.Collections.Generic.Dictionary<string, double>;

namespace EVEMon.Common.MarketPricer.Zkillboard
{
    public sealed class ZkillboardItemPricer : ItemPricer
    {
        #region Fields

        private const string Filename = "zkill_item_prices";
        private const string BaseUrl = "https://zkillboard.com/api/prices/";
        private const int MAX_CONCURRENT = 5;

        private static readonly Queue<int> s_queue = new Queue<int>();
        private static readonly HashSet<int> s_requested = new HashSet<int>();
        private static bool s_queryPending;

        #endregion

        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "zKillboard";

        /// <summary>
        /// Gets a value indicating whether this provider is enabled.
        /// </summary>
        protected override bool Enabled => true;

        /// <summary>
        /// Gets the price by type ID.
        /// </summary>
        public override double GetPriceByTypeID(int id)
        {
            EnsureImportation();

            PriceByItemID.TryGetValue(id, out double result);
            lock (s_queue)
            {
                if (!s_requested.Contains(id))
                {
                    s_requested.Add(id);
                    s_queue.Enqueue(id);
                    if (!s_queryPending)
                    {
                        s_queryPending = true;
                        Task.WhenAll(QueryIDs());
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Ensures the importation from cache.
        /// </summary>
        private void EnsureImportation()
        {
            if (s_queryPending)
                return;

            if (!string.IsNullOrWhiteSpace(SelectedProviderName))
            {
                if (SelectedProviderName != Name)
                {
                    Loaded = false;
                    SelectedProviderName = Name;
                }
            }
            else
                SelectedProviderName = Name;

            string file = LocalXmlCache.GetFileInfo(Filename).FullName;

            if (Loaded)
                return;

            if (File.Exists(file))
                LoadFromFile(file);
            else
            {
                Loaded = true;
                PriceByItemID.Clear();
            }
        }

        /// <summary>
        /// Loads prices from the XML cache file.
        /// </summary>
        private static void LoadFromFile(string file)
        {
            var result = Util.DeserializeXmlFromFile<SerializableECItemPrices>(file);
            PriceByItemID.Clear();
            Loaded = false;
            s_requested.Clear();

            foreach (SerializableECItemPriceListItem item in result.ItemPrices)
                PriceByItemID[item.ID] = item.Prices.Average;
        }

        /// <summary>
        /// Queries queued type IDs from the zKillboard API.
        /// Processes items in small batches with concurrent requests.
        /// </summary>
        private async Task QueryIDs()
        {
            while (true)
            {
                List<int> batch;
                lock (s_queue)
                {
                    if (s_queue.Count == 0)
                    {
                        s_queryPending = false;
                        Loaded = true;
                        break;
                    }
                    batch = new List<int>();
                    for (int i = 0; i < MAX_CONCURRENT && s_queue.Count > 0; i++)
                        batch.Add(s_queue.Dequeue());
                }

                // Fetch prices concurrently for this batch
                var tasks = batch.Select(FetchSinglePrice).ToArray();
                await Task.WhenAll(tasks);
            }

            AppServices.EventAggregator?.Publish(CommonEvents.ItemPricesUpdatedEvent.Instance);

            // Persist to cache
            SaveAsync(Filename, Util.SerializeToXmlDocument(Export())).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches the price for a single type ID from zKillboard.
        /// The API returns historical daily prices; we extract the most recent.
        /// </summary>
        private static async Task FetchSinglePrice(int typeId)
        {
            try
            {
                var url = new Uri($"{BaseUrl}{typeId}/");
                var result = await Util.DownloadJsonAsync<ZkillPriceHistory>(url,
                    new RequestParams { AcceptEncoded = true });

                if (result == null || result.HasError || result.Result == null)
                {
                    AppServices.TraceService?.Trace(
                        $"zKillboard price fetch failed for type {typeId}: {result?.ErrorMessage}",
                        printMethod: false);
                    return;
                }

                // Find the most recent date entry (skip non-date keys like "typeID")
                double latestPrice = 0;
                string latestDate = "";

                foreach (var kvp in result.Result)
                {
                    // Date keys are "YYYY-MM-DD" format (10 chars, starts with digit)
                    if (kvp.Key.Length == 10 && char.IsDigit(kvp.Key[0]))
                    {
                        if (string.CompareOrdinal(kvp.Key, latestDate) > 0)
                        {
                            latestDate = kvp.Key;
                            latestPrice = kvp.Value;
                        }
                    }
                }

                if (latestPrice > 0)
                    PriceByItemID[typeId] = latestPrice;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"zKillboard price error for type {typeId}: {ex.Message}",
                    printMethod: false);
            }
        }

        /// <summary>
        /// Exports the cache to a serializable object for XML persistence.
        /// </summary>
        private static SerializableECItemPrices Export()
        {
            var entitiesList = PriceByItemID
                .OrderBy(x => x.Key)
                .Select(item => new SerializableECItemPriceListItem
                {
                    ID = item.Key,
                    Prices = new SerializableECItemPriceItem { Average = item.Value }
                });

            var serial = new SerializableECItemPrices();
            serial.ItemPrices.AddRange(entitiesList);
            return serial;
        }
    }
}
