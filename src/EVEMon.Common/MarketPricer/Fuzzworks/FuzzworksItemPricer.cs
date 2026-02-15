using EVEMon.Common.Collections;
using EVEMon.Common.Constants;
using EVEMon.Common.Net;
using EVEMon.Common.Serialization;
using EVEMon.Common.Serialization.EveMarketer.MarketPricer;
using EVEMon.Common.Serialization.Fuzzworks;
using EVEMon.Common.Service;
using EVEMon.Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FuzzworksResult = System.Collections.Generic.Dictionary<string, EVEMon.Common.
    Serialization.Fuzzworks.SerializableFuzzworksPriceItem>;

namespace EVEMon.Common.MarketPricer.Fuzzworks
{
    public sealed class FuzzworksItemPricer : ItemPricer
    {
        #region Fields

        private const string Filename = "fuzzy_item_prices";
        private const int MAX_QUERY = 60;

        private static readonly Queue<int> s_queue = new Queue<int>();
        private static readonly HashSet<int> s_requested = new HashSet<int>();
        private static bool s_queryPending;

        #endregion


        /// <summary>
        /// Gets the name.
        /// </summary>
        public override string Name => "Fuzzworks";

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
        /// Imports the specified item prices.
        /// </summary>
        /// <param name="itemPrices">The item prices.</param>
        private static void Import(IDictionary<string, SerializableFuzzworksPriceItem> itemPrices)
        {
            foreach (var pair in itemPrices)
                // IDs in JSON cannot be integers
                if (int.TryParse(pair.Key, out int id))
                {
                    var item = pair.Value;

                    if (item.Sell != null)
                        // JSV Min
                        PriceByItemID[id] = item.Sell.MinPrice;
                    else if (item.Buy != null)
                        // JBV Max
                        PriceByItemID[id] = item.Buy.MaxPrice;
                }
        }

        /// <summary>
        /// Queries the ids.
        /// </summary>
        /// <returns></returns>
        private async Task QueryIDs()
        {
            var idsToQuery = new List<int>();

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

                var url = new Uri(NetworkConstants.FuzzworksMarketUrl + GetQueryString(
                    idsToQuery));
                var result = await Util.DownloadJsonAsync<FuzzworksResult>(url,
                    new RequestParams()
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
                sb.Append(i);

                if (idsToQuery.Last() != i)
                    sb.Append(",");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Called when prices downloaded.
        /// </summary>
        /// <param name="result">The result.</param>
        private void OnPricesDownloaded(JsonResult<FuzzworksResult> result)
        {
            if (CheckQueryStatus(result))
                return;

            if (EveMonClient.IsDebugBuild)
                AppServices.TraceService?.Trace($"Remaining ids: {string.Join(", ", s_queue)}", printMethod: false);

            Loaded = true;

            // Reset query pending flag
            s_queryPending = false;

            AppServices.TraceService?.Trace("done");

            EveMonClient.OnPricesDownloaded(null, string.Empty);

            // Save the file in cache
            SaveAsync(Filename, Util.SerializeToXmlDocument(Export())).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks the query status.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private bool CheckQueryStatus(JsonResult<FuzzworksResult> result)
        {
            if (result == null || result.HasError)
            {
                // Abort further attempts if it is a connection issue
                if (result != null)
                {
                    AppServices.TraceService?.Trace(result.ErrorMessage);
                    s_queue.Clear();

                    // Reset query pending flag
                    s_queryPending = false;
                    EveMonClient.OnPricesDownloaded(null, string.Empty);

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
            else
                // When the query succeeds import the data
                Import(result.Result);

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
