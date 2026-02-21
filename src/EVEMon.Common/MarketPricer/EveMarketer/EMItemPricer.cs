// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Collections;
using EVEMon.Common.Constants;
using EVEMon.Common.Data;
using EVEMon.Common.Net;
using EVEMon.Common.Serialization.EveMarketer.MarketPricer;
using EVEMon.Common.Service;
using EVEMon.Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common.MarketPricer.EveMarketer
{
    public sealed class EMItemPricer : ItemPricer
    {
        #region Fields

        private const string Filename = "ec_item_prices";
        private const int MAX_QUERY = 60;

        private static readonly Queue<int> s_queue = new Queue<int>();
        private static readonly HashSet<int> s_requested = new HashSet<int>();
        private static bool s_queryPending;

        #endregion


        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "EVEMarketer";

        /// <summary>
        /// Gets a value indicating whether this <see cref="ItemPricer" /> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        protected override bool Enabled => true;

        /// <summary>
        /// Gets the price by type ID.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public override double GetPriceByTypeID(int id)
        {
            // Ensure list importation
            EnsureImportation();

            double result;
            PriceByItemID.TryGetValue(id, out result);
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
        /// Ensures the importation.
        /// </summary>
        private void EnsureImportation()
        {
            // Quit if query is pending
            if (s_queryPending)
                return;

            // Check the selected provider
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

            // Exit if we have already imported the list
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
        /// Loads from file.
        /// </summary>
        /// <param name="file">The file.</param>
        private void LoadFromFile(string file)
        {
            // Deserialize the xml file
            var result = Util.DeserializeXmlFromFile<SerializableECItemPrices>(file);

            PriceByItemID.Clear();
            Loaded = false;
            s_requested.Clear();

            // Import the data
            Import(result.ItemPrices);
        }

        /// <summary>
        /// Imports the specified item prices.
        /// </summary>
        /// <param name="itemPrices">The item prices.</param>
        private static void Import(IEnumerable<SerializableECItemPriceListItem> itemPrices)
        {
            foreach (SerializableECItemPriceListItem item in itemPrices)
            {
                PriceByItemID[item.ID] = item.Prices.Average;
            }
        }

        /// <summary>
        /// Queries the ids.
        /// </summary>
        /// <returns></returns>
        private async Task QueryIDs()
        {
            var idsToQuery = new List<int>();
            var url = new Uri(NetworkConstants.EVEMarketerBaseUrl + NetworkConstants.
                EVEMarketerAPIItemPrices);

            while (true)
            {
                lock (s_queue)
                {
                    // Cannot await inside lock, this is the cleanest way to do it
                    if (s_queue.Count == 0)
                        return;
                    idsToQuery.Clear();
                    for (int i = 0; i < MAX_QUERY && s_queue.Count > 0; i++)
                        idsToQuery.Add(s_queue.Dequeue());
                }

                var result = await Util.DownloadXmlAsync<SerializableECItemPrices>(url,
                    new RequestParams(GetQueryString(idsToQuery))
                    {
                        AcceptEncoded = true
                    });
                OnPricesDownloaded(result);
            }
        }

        /// <summary>
        /// Gets the query string.
        /// </summary>
        /// <param name="idsToQuery">The ids to query.</param>
        /// <returns></returns>
        private static string GetQueryString(IReadOnlyCollection<int> idsToQuery)
        {
            var sb = new StringBuilder(256);
            foreach (int i in idsToQuery)
            {
                sb.Append($"typeid={i}");

                if (idsToQuery.Last() != i)
                    sb.Append("&");
            }
            var jitaSolarSystem = StaticGeography.GetSolarSystemByName("Jita");
            if (jitaSolarSystem != null)
                sb.Append($"&usesystem={jitaSolarSystem.ID}");
            return sb.ToString();
        }

        /// <summary>
        /// Called when prices downloaded.
        /// </summary>
        /// <param name="result">The result.</param>
        private void OnPricesDownloaded(DownloadResult<SerializableECItemPrices> result)
        {
            if (CheckQueryStatus(result))
                return;

            if (EveMonClient.IsDebugBuild)
                AppServices.TraceService?.Trace($"Remaining ids: {string.Join(", ", s_queue)}", printMethod: false);

            Loaded = true;

            // Reset query pending flag
            s_queryPending = false;

            AppServices.TraceService?.Trace("done");

            AppServices.EventAggregator?.Publish(CommonEvents.ItemPricesUpdatedEvent.Instance);

            // Save the file in cache
            SaveAsync(Filename, Util.SerializeToXmlDocument(Export())).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks the query status.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private bool CheckQueryStatus(DownloadResult<SerializableECItemPrices> result)
        {
            if (result?.Error != null)
            {
                AppServices.TraceService?.Trace(result.Error.Message);

                // Abort further attempts if it is a connection issue
                if (result.Error.Status == HttpWebClientServiceExceptionStatus.Timeout ||
                    result.Error.Status == HttpWebClientServiceExceptionStatus.ServerError)
                {
                    s_queue.Clear();

                    // Reset query pending flag
                    s_queryPending = false;
                    AppServices.EventAggregator?.Publish(CommonEvents.ItemPricesUpdatedEvent.Instance);

                    // We return 'true' to avoid saving a file
                    return true;
                }

                lock (s_queue)
                {
                    // If we are done set the proper flags
                    if (s_queue.Count < 1)
                    {
                        Loaded = true;
                        AppServices.TraceService?.Trace("ECItemPricer.Import - done", printMethod: false);
                        return false;
                    }
                }
            }

            // When the query succeeds import the data
            if (result?.Result != null)
                Import(result.Result.ItemPrices);

            // If all items where queried we are done (false = save file)
            bool hasMore;
            lock (s_queue)
            {
                hasMore = s_queue.Count > 0;
            }

            return hasMore;
        }

        /// <summary>
        /// Exports the cache list to a serializable object.
        /// </summary>
        /// <returns></returns>
        private static SerializableECItemPrices Export()
        {
            IEnumerable<SerializableECItemPriceListItem> entitiesList = PriceByItemID
                .OrderBy(x => x.Key)
                .Select(
                    item =>
                        new SerializableECItemPriceListItem
                        {
                            ID = item.Key,
                            Prices = new SerializableECItemPriceItem { Average = item.Value }
                        });

            SerializableECItemPrices serial = new SerializableECItemPrices();
            serial.ItemPrices.AddRange(entitiesList);

            return serial;
        }
    }
}
