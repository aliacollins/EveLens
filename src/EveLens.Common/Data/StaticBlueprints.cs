// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Collections.Global;
using EveLens.Common.Serialization.Datafiles;
using EveLens.Core;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents the list of all static blueprints.
    /// </summary>
    public static class StaticBlueprints
    {
        private static readonly Dictionary<int, Blueprint> s_blueprintsByID = new Dictionary<int, Blueprint>();


        #region Initialization

        /// <summary>
        /// Initialize static blueprints.
        /// </summary>
        public static void Load()
        {
            if (BlueprintMarketGroups != null)
                return;

            BlueprintsDatafile datafile = Util.DeserializeDatafile<BlueprintsDatafile>(DatafileConstants.BlueprintsDatafile,
                Util.LoadXslt(ServiceLocator.ResourceProvider.DatafilesXSLT));

            BlueprintMarketGroups = new BlueprintMarketGroupCollection(null, datafile.MarketGroups);

            foreach (BlueprintMarketGroup srcGroup in BlueprintMarketGroups)
            {
                InitializeDictionaries(srcGroup);
            }

            GlobalDatafileCollection.OnDatafileLoaded();
        }

        /// <summary>
        /// Recursively collect the blueprints within all groups and stores them in the dictionaries.
        /// </summary>
        /// <param name="marketGroup"></param>
        private static void InitializeDictionaries(BlueprintMarketGroup marketGroup)
        {
            foreach (Blueprint blueprint in marketGroup.Blueprints)
            {
                s_blueprintsByID[blueprint.ID] = blueprint;
            }

            foreach (BlueprintMarketGroup childGroup in marketGroup.SubGroups)
            {
                InitializeDictionaries(childGroup);
            }
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the root category, containing all the top level categories
        /// </summary>
        public static BlueprintMarketGroupCollection BlueprintMarketGroups { get; private set; }

        /// <summary>
        /// Gets the collection of all the blueprints in this category and its descendants.
        /// </summary>
        public static IEnumerable<Blueprint> AllBlueprints => s_blueprintsByID.Values;

        #endregion


        #region Public Finders

        /// <summary>
        /// Recursively searches the root category and all underlying categories
        /// for the first blueprint with an Id matching the given blueprintId.
        /// </summary>
        /// <param name="blueprintId">The id of the blueprint to find.</param>
        /// <returns>The first blueprint which id matches blueprintId, Null if no such blueprint is found.</returns>
        public static Blueprint GetBlueprintByID(int blueprintId)
        {
            Blueprint value;
            s_blueprintsByID.TryGetValue(blueprintId, out value);
            return value;
        }

        /// <summary>
        /// Recursively searches the root category and all underlying categories
        /// for the first blueprint with a name matching the given blueprintId.
        /// </summary>
        /// <param name="blueprintName">The name of the blueprint to find.</param>
        /// <returns>The first blueprint which name matches blueprintName, Null if no such blueprint is found.</returns>
        public static Blueprint GetBlueprintByName(string blueprintName)
            => s_blueprintsByID.Values.FirstOrDefault(x => x.Name == blueprintName);

        #endregion
    }
}