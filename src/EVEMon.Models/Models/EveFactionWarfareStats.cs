using EVEMon.Common.Serialization.Eve;

namespace EVEMon.Common.Models
{
    public class EveFactionWarfareStats
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EveFactionWarfareStats"/> class.
        /// </summary>
        /// <param name="src">The source.</param>
        internal EveFactionWarfareStats(SerializableEveFactionalWarfareStatsListItem src)
        {
            FactionID = src.FactionID;
            FactionName = src.FactionName;
            Pilots = src.Pilots;
            SystemsControlled = src.SystemsControlled;
            KillsYesterday = src.KillsYesterday;
            KillsLastWeek = src.KillsLastWeek;
            KillsTotal = src.KillsTotal;
            VictoryPointsYesterday = src.VictoryPointsYesterday;
            VictoryPointsLastWeek = src.VictoryPointsLastWeek;
            VictoryPointsTotal = src.VictoryPointsTotal;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets or sets the faction ID.
        /// </summary>
        public int FactionID { get; }

        /// <summary>
        /// Gets or sets the name of the faction.
        /// </summary>
        public string FactionName { get; set; }

        /// <summary>
        /// Gets or sets the pilots.
        /// </summary>
        public int Pilots { get; }

        /// <summary>
        /// Gets or sets the systems controlled.
        /// </summary>
        public int SystemsControlled { get; }

        /// <summary>
        /// Gets or sets the kills yesterday.
        /// </summary>
        public int KillsYesterday { get; }

        /// <summary>
        /// Gets or sets the kills last week.
        /// </summary>
        public int KillsLastWeek { get; }

        /// <summary>
        /// Gets or sets the kills total.
        /// </summary>
        public int KillsTotal { get; }

        /// <summary>
        /// Gets or sets the victory points yesterday.
        /// </summary>
        public int VictoryPointsYesterday { get; }

        /// <summary>
        /// Gets or sets the victory points last week.
        /// </summary>
        public int VictoryPointsLastWeek { get; }

        /// <summary>
        /// Gets or sets the victory points total.
        /// </summary>
        public int VictoryPointsTotal { get; }

        #endregion

    }
}