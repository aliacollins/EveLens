// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Serialization.Datafiles;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// The static list of the masteries.
    /// </summary>
    public static class StaticMasteries
    {
        private static readonly Dictionary<int, MasteryShip> s_masteryShipsByID =
            new Dictionary<int, MasteryShip>();


        #region Initialization

        /// <summary>
        /// Initialize static masteries.
        /// </summary>
        public static void Load()
        {
            MasteriesDatafile datafile = Util.DeserializeDatafile<MasteriesDatafile>(
                DatafileConstants.MasteriesDatafile);

            foreach (SerializableMasteryShip srcShip in datafile.MasteryShips)
            {
                Ship ship = StaticItems.GetItemByID(srcShip.ID) as Ship;
                if (ship != null)
                    s_masteryShipsByID[ship.ID] = new MasteryShip(srcShip, ship);
            }

            GlobalDatafileCollection.OnDatafileLoaded();
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets an enumeration of all the mastery ships.
        /// </summary>
        public static IEnumerable<MasteryShip> AllMasteryShips => s_masteryShipsByID.Values;

        #endregion

    }
}
