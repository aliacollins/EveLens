using System;
using System.Linq;
using System.Threading.Tasks;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Threading;

namespace EVEMon.Common.QueryMonitor
{
    /// <summary>
    /// Represents a monitor for all queries related to characters and their corporations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CCPQueryMonitorBase<T> : QueryMonitor<T> where T : class
    {
        protected readonly CCPCharacter m_character;
        protected ESIKey m_apiKey;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character">The character to monitor.</param>
        /// <param name="method">The method to use.</param>
        /// <param name="onSuccess">The callback to use on success or failure.</param>
        internal CCPQueryMonitorBase(CCPCharacter character, Enum method,
            Action<EsiResult<T>> callback, bool suppressSelfTicking = false) : base(method, callback, suppressSelfTicking)
        {
            m_character = character;
        }

        /// <summary>
        /// Retrieves the parameters required for the ESI request.
        /// </summary>
        /// <returns>The ESI request parameters.</returns>
        internal abstract ESIParams GetESIParams();

        /// <summary>
        /// Gets the required API key information are known.
        /// </summary>
        /// <returns>False if an API key was required and not found.</returns>
        internal override bool HasESIKey => m_character.Identity.ESIKeys.Any();

        /// <summary>
        /// Performs the query to the provider asynchronously using modern async/await pattern.
        /// </summary>
        /// <param name="provider">The API provider to use.</param>
        protected override async Task QueryAsyncCoreAsync(APIProvider provider)
        {
            provider.ThrowIfNull(nameof(provider));

            try
            {
                var result = await provider.QueryEsiAsync<T>(Method, GetESIParams())
                    .ConfigureAwait(false);

                // Marshal back to UI thread and call OnQueried for proper bookkeeping
                Dispatcher.Invoke(() => OnQueried(result));
            }
            catch (Exception ex)
            {
                // Ensure IsUpdating is reset even if an exception occurs
                Dispatcher.Invoke(() => ResetUpdatingState(ex));
            }
        }

    }
}
