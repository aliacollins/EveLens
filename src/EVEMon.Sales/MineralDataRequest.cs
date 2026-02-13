using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EVEMon.Common.Data;
using EVEMon.Common.Helpers;
using EVEMon.Common.Net;

namespace EVEMon.Sales
{
    /// <summary>
    /// Gets mineral data from parsers.
    /// </summary>
    public static class MineralDataRequest
    {
        private static readonly Dictionary<string, IMineralParser> s_parsers = new Dictionary<string, IMineralParser>();

        /// <summary>
        /// Initializes the data source.
        /// </summary>
        internal static void Initialize()
        {
            foreach (IMineralParser parser in Assembly.GetExecutingAssembly().GetTypes().Where(
                type => typeof(IMineralParser).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null).Select(
                    type => Activator.CreateInstance(type) as IMineralParser).OrderBy(parser => parser.Name))
            {
                s_parsers[parser.Name] = parser;
            }
        }

        /// <summary>
        /// Gets the parsers.
        /// </summary>
        /// <value>The parsers.</value>
        public static IEnumerable<IMineralParser> Parsers => s_parsers.Values;

        /// <summary>
        /// The prices of mineral from the given source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>An enumerable collection of Minerals and Prices.</returns>
        public static IEnumerable<MineralPrice> Prices(string source)
        {
            if (!s_parsers.ContainsKey(source))
                throw new ArgumentException(@"That is not a registered mineral datasource.", nameof(source));

            IMineralParser parser = s_parsers[source];
            return GetPrices(parser);
        }

        /// <summary>
        /// Gets the courtesy text.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The courtesy text.</returns>
        public static string GetCourtesyText(string source)
        {
            if (!s_parsers.ContainsKey(source))
                throw new ArgumentException(@"That is not a registered mineral datasource.", nameof(source));

            IMineralParser p = s_parsers[source];
            return p.CourtesyText;
        }

        /// <summary>
        /// Gets the courtesy URL.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The courtesy URL.</returns>
        public static Uri GetCourtesyUrl(string source)
        {
            if (!s_parsers.ContainsKey(source))
                throw new ArgumentException(@"That is not a registered mineral datasource.", nameof(source));

            IMineralParser p = s_parsers[source];
            return p.CourtesyUrl;
        }

        /// <summary>
        /// Scans for prices.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<MineralPrice> GetPrices(IMineralParser parser)
        {
            string content = String.Empty;
            try
            {
                // Called from BackgroundWorker.DoWork (thread pool) - blocking is safe here.
                // Uses async method directly to avoid the [Obsolete] sync wrapper.
#pragma warning disable VSTHRD002 // Synchronously waiting on tasks in background worker
                var downloadResult = HttpWebClientService.DownloadStringAsync(parser.URL)
                    .GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
                content = downloadResult.Result;
            }
            catch (HttpWebClientServiceException ex)
            {
                ExceptionHandler.LogException(ex, false);
            }

            // Scan for prices
            MatchCollection mc = parser.Tokenizer.Matches(content);

            return mc.Cast<Match>().Select(match =>
            {
                int typeID;
                string name = Int32.TryParse(match.Groups["name"].Value, out typeID)
                    ? StaticItems.GetItemByID(typeID).Name
                    : match.Groups["name"].Value;

                return new MineralPrice
                {
                    Name = name,
                    Price = Decimal.Parse(match.Groups["price"].Value,
                        NumberStyles.Currency,
                        CultureInfo.InvariantCulture)
                };
            });
        }
    }
}
