// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Attributes;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Represents a certificate from a character's point of view.
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class Certificate
    {
        private readonly Character m_character;
        private readonly CertificateLevel[] m_levels;


        #region Initialization, importation, exportation and update

        /// <summary>
        /// Constructor at character's initialization time.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="src"></param>
        /// <param name="certClass"></param>
        internal Certificate(Character character, StaticCertificate src, CertificateClass certClass)
        {
            m_character = character;
            m_levels = new CertificateLevel[6];

            StaticData = src;
            Class = certClass;

            foreach (KeyValuePair<CertificateGrade, List<StaticSkillLevel>> skill in src.PrerequisiteSkills)
            {
                m_levels[(int)skill.Key] = new CertificateLevel(skill, this, character);
            }
        }

        #endregion


        #region Core Properties

        /// <summary>
        /// Gets the static data associated with this certificate.
        /// </summary>
        private StaticCertificate StaticData { get; }

        /// <summary>
        /// Gets this certificate's id.
        /// </summary>
        public int ID => StaticData.ID;

        /// <summary>
        /// Gets this certificate's name.
        /// </summary>
        public string Name => StaticData.Class.Name;

        /// <summary>
        /// Gets this certificate's description.
        /// </summary>
        public string Description => StaticData.Description;

        /// <summary>
        /// Gets the class for this certificate.
        /// </summary>
        public CertificateClass Class { get; }

        /// <summary>
        /// Gets the ships this certificate is recommended for.
        /// </summary>
        public IEnumerable<Item> Recommendations => StaticData.Recommendations;

        /// <summary>
        /// Gets all levels of the cerificate.
        /// </summary>
        /// <value>
        /// All level.
        /// </value>
        public IEnumerable<CertificateLevel> AllLevel => m_levels.Where(level => level != null);

        /// <summary>
        /// Gets all the top-level prerequisite skills.
        /// </summary>
        public IEnumerable<SkillLevel> AllTopPrerequisiteSkills => StaticData.AllTopPrerequisiteSkills.ToCharacter(m_character);

        /// <summary>
        /// Gets the required training time for the provided character to train this certificate.
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetTrainingTime => m_character.GetTrainingTimeToMultipleSkills(AllTopPrerequisiteSkills);

        /// <summary>
        /// Gets the lowest untrained certificate level.
        /// Null if all certificates have been trained.
        /// </summary>
        public CertificateLevel LowestUntrainedLevel => AllLevel.FirstOrDefault(cert => !cert.IsTrained);

        /// <summary>
        /// Gets the highest trained certificate level.
        /// Null if no certificates have been trained.
        /// </summary>
        public CertificateLevel HighestTrainedLevel => AllLevel.LastOrDefault(cert => cert.IsTrained);

        #endregion


        #region Helper Methods

        /// <summary>
        /// Gets the certificate level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns></returns>
        public CertificateLevel GetCertificateLevel(int level) => AllLevel.FirstOrDefault(x => (int)x.Level == level);

        /// <summary>
        /// Try to update the certificate's status. 
        /// </summary>
        /// <returns>True if the status was updated, false otherwise.</returns>
        internal bool TryUpdateCertificateStatus()
            => AllLevel.Aggregate(false, (current, level) => current | level.TryUpdateCertificateStatus());

        #endregion


        /// <summary>
        /// Gets a string representation of this certificate.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => StaticData.ToString();

        /// <summary>
        /// Implicit conversion operator to the static equivalent of this certificate.
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        public static implicit operator StaticCertificate(Certificate cert) => cert?.StaticData;
    }
}