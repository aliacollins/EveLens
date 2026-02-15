﻿using System.Xml.Serialization;
﻿using EVEMon.Common.Enumerations;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a skill prerequisite for a certificate
    /// </summary>
    public sealed class SerializableCertificatePrerequisite
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("id")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the skill.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute("skill")]
        public string? Skill { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [XmlAttribute("level")]
        public string? Level { get; set; }

        /// <summary>
        /// Gets or sets the grade.
        /// </summary>
        /// <value>The grade.</value>
        [XmlAttribute("grade")]
        public CertificateGrade Grade { get; set; }
    }
}