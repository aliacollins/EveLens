using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class DgmTypeMasteries : IRelation
    {
        [XmlElement("typeID")]
        public int ItemID { get; set; }

        [XmlElement("masteryID")]
        public int MasteryID { get; set; }


        #region IRelation Members

        /// <summary>
        /// Gets the left column value.
        /// </summary>
        /// <value>The left.</value>
        int IRelation.Left => ItemID;

        /// <summary>
        /// Gets the center column value.
        /// </summary>
        /// <value>
        /// The center.
        /// </value>
        int IRelation.Center => 0;

        /// <summary>
        /// Gets the right column value.
        /// </summary>
        /// <value>The right.</value>
        int IRelation.Right => MasteryID;

        #endregion
    }
}
