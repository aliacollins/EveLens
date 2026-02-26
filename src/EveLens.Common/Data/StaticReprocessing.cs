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
    /// Stores all the data regarding reprocessing.
    /// </summary>
    public static class StaticReprocessing
    {
        private static readonly Dictionary<int, MaterialCollection> s_itemMaterialsByID = new Dictionary<int, MaterialCollection>();

        /// <summary>
        /// Initialize static reprocssing information.
        /// </summary>
        public static void Load()
        {
            ReprocessingDatafile datafile = Util.DeserializeDatafile<ReprocessingDatafile>(
                DatafileConstants.ReprocessingDatafile, Util.LoadXslt(ServiceLocator.ResourceProvider.DatafilesXSLT));

            foreach (SerializableItemMaterials item in datafile.Items)
            {
                MaterialCollection materials = new MaterialCollection(item.Materials.Select(itemMaterial => new Material(itemMaterial)).ToList());
                s_itemMaterialsByID[item.ID] = materials;
            }

            GlobalDatafileCollection.OnDatafileLoaded();
        }

        /// <summary>
        /// Gets an enumeration of all the reprocessing materials.
        /// </summary>
        public static IEnumerable<MaterialCollection> AllReprocessingMaterials => s_itemMaterialsByID.Values;

        /// <summary>
        /// Gets the materials for the provided itemID.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public static IEnumerable<Material> GetItemMaterialsByID(int id)
        {
            MaterialCollection result;
            s_itemMaterialsByID.TryGetValue(id, out result);
            return result;
        }
    }
}