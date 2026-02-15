using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{     

    /// <summary>
    /// Represents a serializable version of a character's sheet. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPICharacterSheet : SerializableCharacterSheetBase
    {
        private readonly Collection<SerializableNewImplant> m_implants;
        private readonly Collection<SerializableCharacterJumpClone> m_jumpClones;
        private readonly Collection<SerializableCharacterJumpCloneImplant> m_jumpCloneImplants;

        public SerializableAPICharacterSheet()
        {
            m_implants = new Collection<SerializableNewImplant>();
            m_jumpClones = new Collection<SerializableCharacterJumpClone>();
            m_jumpCloneImplants = new Collection<SerializableCharacterJumpCloneImplant>();
        }

        [XmlArray("implants")]
        [XmlArrayItem("implant")]
        public Collection<SerializableNewImplant> Implants => m_implants;

        [XmlArray("jumpClones")]
        [XmlArrayItem("jumpClone")]
        public Collection<SerializableCharacterJumpClone> JumpClones => m_jumpClones;

        [XmlArray("jumpCloneImplants")]
        [XmlArrayItem("jumpCloneImplant")]
        public Collection<SerializableCharacterJumpCloneImplant> JumpCloneImplants => m_jumpCloneImplants;
    }
}