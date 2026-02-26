// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Collections.Global;
using EveLens.Common.Constants;
using EveLens.Common.Enumerations;
using EveLens.Common.Serialization.Datafiles;
using EveLens.Core;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents all the items loaded from the datafiles. 
    /// </summary>
    public static class StaticItems
    {
        private static readonly Dictionary<int, MarketGroup> s_marketGroupsByID = new Dictionary<int, MarketGroup>();
        private static readonly Dictionary<int, Item> s_itemsByID = new Dictionary<int, Item>();
        private static readonly ImplantCollection[] s_implantSlots = new ImplantCollection[10];


        #region Initialization

        /// <summary>
        /// Initialize static items.
        /// </summary>
        public static void Load()
        {
            if (MarketGroups != null)
                return;

            // Create the implants slots
            for (int i = 0; i < s_implantSlots.Length; i++)
            {
                s_implantSlots[i] = new ImplantCollection((ImplantSlots)i);
            }

            // Deserialize the items datafile
            ItemsDatafile datafile = Util.DeserializeDatafile<ItemsDatafile>(DatafileConstants.ItemsDatafile,
                Util.LoadXslt(ServiceLocator.ResourceProvider.DatafilesXSLT));

            MarketGroups = new MarketGroupCollection(null, datafile.MarketGroups);

            // Gather the items into a by-ID dictionary
            foreach (MarketGroup marketGroup in MarketGroups)
            {
                InitializeDictionaries(marketGroup);
            }

            GlobalDatafileCollection.OnDatafileLoaded();
        }

        /// <summary>
        /// Recursively collect the items within all groups and stores them in the dictionaries.
        /// </summary>
        /// <param name="marketGroup"></param>
        private static void InitializeDictionaries(MarketGroup marketGroup)
        {
            // Special groups
            if (marketGroup.ID == DBConstants.ShipsMarketGroupID)
                ShipsMarketGroup = marketGroup;

            s_marketGroupsByID[marketGroup.ID] = marketGroup;

            foreach (Item item in marketGroup.Items)
            {
                s_itemsByID[item.ID] = item;
            }

            foreach (MarketGroup childGroup in marketGroup.SubGroups)
            {
                InitializeDictionaries(childGroup);
            }
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the root category, containing all the top level categories.
        /// </summary>
        public static MarketGroupCollection MarketGroups { get; private set; }

        /// <summary>
        /// Gets the market group for ships.
        /// </summary>
        public static MarketGroup ShipsMarketGroup { get; private set; }

        /// <summary>
        /// Gets the collection of all the market groups in this category and its descendants.
        /// </summary>
        public static IEnumerable<MarketGroup> AllGroups => s_marketGroupsByID.Values;

        /// <summary>
        /// Gets the collection of all the items in this category and its descendants.
        /// </summary>
        public static IEnumerable<Item> AllItems => s_itemsByID.Values;

        #endregion


        #region Public Finders

        /// <summary>
        /// Gets the collection of implants for the given slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static ImplantCollection GetImplants(ImplantSlots slot) => s_implantSlots[(int)slot];

        /// <summary>
        /// Recursively searches the root category and all underlying categories
        /// for the first item with an Id matching the given itemId.
        /// </summary>
        /// <param name="itemId">The id of the item to find.</param>
        /// <returns>The first item which id matches itemId, Null if no such item is found.</returns>
        public static Item GetItemByID(int itemId)
        {
            Item value;
            s_itemsByID.TryGetValue(itemId, out value);
            return value;
        }

        /// <summary>
        /// Shorthand for GetItemByID that returns "unknown" if item is not in the database.
        /// </summary>
        /// <param name="itemId">The id of the item to find.</param>
        /// <returns>The first item name which id matches Item ID, EveLensConstants.UnknownText if no such item is found.</returns>
        public static string GetItemName(int itemId)
        {
            return GetItemByID(itemId)?.Name ?? EveLensConstants.UnknownText;
        }

        /// <summary>
        /// Recursively searches the root category and all underlying categories for the first item with a 
        /// name that exactly matches the given itemName.
        /// </summary>
        /// <param name="itemName">The name of the item to find.</param>
        /// <returns>The first item which name matches itemName, Null if no such item is found.</returns>
        public static Item GetItemByName(string itemName)
            => s_itemsByID.Values.FirstOrDefault(item => item.Name == itemName);

        #endregion
    }
}
