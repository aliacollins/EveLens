using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Attributes;
using EVEMon.Common.Collections;
using EVEMon.Common.Data;
using EVEMon.Common.Serialization.Eve;

namespace EVEMon.Common.Models.Collections
{
    /// <summary>
    /// Represents a collection of skills.
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class SkillCollection : ReadonlyKeyedCollection<int, Skill>
    {
        private readonly Skill[] m_itemsArray = new Skill[StaticSkills.ArrayIndicesCount];
        private static SkillCollection s_skillCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillCollection"/> class.
        /// Used to build a non-character associated skill collection.
        /// </summary>
        public SkillCollection()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillCollection"/> class.
        /// Used to build a character associated skill collection.
        /// </summary>
        /// <param name="character">The character.</param>
        internal SkillCollection(Character character)
        {
            IEnumerable<Skill> skills = character?.SkillGroups.SelectMany(group => group)
                              ?? StaticSkills.AllGroups?.SelectMany(group => new SkillGroup(group));

            if (skills == null)
                return;

            foreach (Skill skill in skills)
            {
                Items[skill.ID] = skill;
                m_itemsArray[skill.ArrayIndex] = skill;
            }

            // Build prerequisites list
            foreach (Skill skill in m_itemsArray)
            {
                skill?.CompleteInitialization(m_itemsArray);
            }
        }

        /// <summary>
        /// Gets a collection of non-character assosiated skills.
        /// </summary>
        /// <value>
        /// The skills.
        /// </value>
        public static SkillCollection Skills => s_skillCollection ?? (s_skillCollection = new SkillCollection());

        /// <summary>
        /// Gets the skill with the provided id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Skill this[int id] => GetByKey(id) ?? Skill.UnknownSkill;

        /// <summary>
        /// Gets the skill with the provided array index (see <see cref="StaticSkill.ArrayIndex"/>).
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Skill GetByArrayIndex(int index) => m_itemsArray[index];

        /// <summary>
        /// Exports this collection to a serialization object.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<SerializableCharacterSkill> Export()
            => Items.Values.Where(x => x.IsKnown || x.IsOwned).Select(skill => skill.Export());

        /// Imports data from a serialization object.
        internal void Import(IEnumerable<SerializableCharacterSkill> skills, bool fromCCP)
        {
            // Skills : reset all > update all
            foreach (Skill skill in Items.Values)
            {
                skill.Reset(fromCCP);
            }

            // Take care of the new skills not in our datafiles yet
            // Update if it exists
            foreach (SerializableCharacterSkill serialSkill in skills.Where(x => this[x.ID] != null && Items.ContainsKey(x.ID)))
            {
                Items[serialSkill.ID].Import(serialSkill, fromCCP);
            }
        }
    }
}