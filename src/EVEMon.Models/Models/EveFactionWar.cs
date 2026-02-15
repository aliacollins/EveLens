using System;
using EVEMon.Common.Constants;
using EVEMon.Common.Serialization.Eve;

namespace EVEMon.Common.Models
{
    public class EveFactionWar
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EveFactionWar"/> class.
        /// </summary>
        /// <param name="src">The source.</param>
        internal EveFactionWar(SerializableEveFactionWarsListItem src)
        {
            FactionID = src.FactionID;
            FactionName = src.FactionName;
            AgainstID = src.AgainstID;
            AgainstName = src.AgainstName;
            PrimeAgainstID = SetPrimeEnemy(src.FactionID);
        }

        #endregion


        #region Public Properties

        public int PrimeAgainstID { get; }

        /// <summary>
        /// Gets or sets the faction ID.
        /// </summary>
        public int FactionID { get; }

        /// <summary>
        /// Gets or sets the name of the faction.
        /// </summary>
        public string FactionName { get; }

        /// <summary>
        /// Gets or sets the against ID.
        /// </summary>
        public int AgainstID { get; }

        /// <summary>
        /// Gets or sets the name of the against.
        /// </summary>
        public string AgainstName { get; }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Sets the prime enemy of a faction.
        /// </summary>
        /// <param name="factionID"></param>
        private static int SetPrimeEnemy(int factionID)
        {
            switch (factionID)
            {
                case DBConstants.AmarrFactionID:
                    return DBConstants.MinmatarFactionID;
                case DBConstants.CaldariFactionID:
                    return DBConstants.GallenteFactionID;
                case DBConstants.GallenteFactionID:
                    return DBConstants.CaldariFactionID;
                case DBConstants.MinmatarFactionID:
                    return DBConstants.AmarrFactionID;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
    }
}