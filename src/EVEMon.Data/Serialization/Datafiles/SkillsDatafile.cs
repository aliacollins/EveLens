using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Root SkillsDatafile Serialization Class
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    [XmlRoot("skills")]
    public sealed class SkillsDatafile
    {
        private readonly Collection<SerializableSkillGroup> m_skillGroups;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillsDatafile"/> class.
        /// </summary>
        public SkillsDatafile()
        {
            m_skillGroups = new Collection<SerializableSkillGroup>();
        }

        /// <summary>
        /// Gets the skill groups.
        /// </summary>
        /// <value>The skill groups.</value>
        [XmlElement("group")]
        public Collection<SerializableSkillGroup> SkillGroups => m_skillGroups;
    }
}