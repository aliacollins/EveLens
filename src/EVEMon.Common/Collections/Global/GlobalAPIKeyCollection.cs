// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common.Collections.Global
{
    public class GlobalAPIKeyCollection : ReadonlyKeyedCollection<long, ESIKey>
    {

        #region Indexer

        /// <summary>
        /// Gets the API key with the provided id, null when not found
        /// </summary>
        /// <value>
        /// The <see cref="ESIKey"/>.
        /// </value>
        /// <param name="id">The id to look for</param>
        /// <returns>
        /// The searched API key when found; null otherwise.
        /// </returns>
        public ESIKey this[long id] => Items.Values.FirstOrDefault(apiKey => apiKey.ID == id);

        #endregion


        #region Addition / Removal Methods

        /// <summary>
        /// Removes the provided API key from this collection.
        /// </summary>
        /// <param name="apiKey">The API key to remove</param>
        /// <exception cref="System.InvalidOperationException">The API key does not exist in the list.</exception>
        /// <exception cref="System.ArgumentNullException">apiKey</exception>
        public void Remove(ESIKey apiKey)
        {
            apiKey.ThrowIfNull(nameof(apiKey));

            // Removes the API key on the owned identities
            foreach (CharacterIdentity identity in apiKey.CharacterIdentities.Where(x => x.ESIKeys.Contains(apiKey)))
            {
                identity.ESIKeys.Remove(apiKey);

                if (identity.CCPCharacter != null)
                {
                    var character = identity.CCPCharacter;
                    AppServices.TraceService?.Trace(character.Name);
                    AppServices.EventAggregator?.Publish(new CharacterUpdatedEvent(character.CharacterID, character.Name));
                    AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpdatedEvent(character));
                }
            }

            // Remove the API key
            if (!Items.Remove(apiKey.ID))
                throw new InvalidOperationException("This API key does not exist in the list.");

            // Dispose
            apiKey.Dispose();

            AppServices.TraceService?.Trace("ESIKeyCollectionChanged");
            AppServices.EventAggregator?.Publish(ESIKeyCollectionChangedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// Adds an API key to this collection.
        /// </summary>
        /// <param name="apiKey"></param>
        internal void Add(ESIKey apiKey)
        {
            Items.Add(apiKey.ID, apiKey);
            AppServices.TraceService?.Trace("ESIKeyCollectionChanged");
            AppServices.EventAggregator?.Publish(ESIKeyCollectionChangedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyCollectionChangedEvent.Instance);
        }

        #endregion


        #region Import / Export Methods

        /// <summary>
        /// Imports the serialized API key.
        /// </summary>
        /// <param name="serial"></param>
        internal void Import(IEnumerable<SerializableESIKey> serial)
        {
            // Unsubscribe events
            foreach (ESIKey apiKey in Items.Values)
            {
                apiKey.Dispose();
            }

            Items.Clear();
            foreach (SerializableESIKey apikey in serial)
            {
                try
                {
                    Items.Add(apikey.ID, new ESIKey(apikey));
                }
                catch (ArgumentException ex)
                {
                    AppServices.TraceService?.Trace($"An API key with id {apikey.ID} already existed; additional instance ignored.");
                    ExceptionHandler.LogException(ex, true);
                }
            }

            AppServices.TraceService?.Trace("ESIKeyCollectionChanged");
            AppServices.EventAggregator?.Publish(ESIKeyCollectionChangedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// Exports the data to a serialization object.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<SerializableESIKey> Export() => Items.Values.Select(apikey => apikey.Export());

        #endregion
    }
}