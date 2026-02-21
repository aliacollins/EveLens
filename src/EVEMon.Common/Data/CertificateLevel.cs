// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Wraps the informations of a specific level of a certificate
    /// </summary>
    public sealed class CertificateLevel
    {
        private bool m_initialized;

        private readonly Character m_character;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateLevel"/> class.
        /// </summary>
        /// <param name="skill">The skill.</param>
        /// <param name="cert">The cert.</param>
        /// <param name="character">The character.</param>
        public CertificateLevel(KeyValuePair<CertificateGrade, List<StaticSkillLevel>> skill, Certificate cert,
            Character character)
        {
            m_character = character;

            Level = skill.Key;
            Certificate = cert;
            Status = CertificateStatus.Untrained;
            PrerequisiteSkills = skill.Value.ToCharacter(character);
        }

        /// <summary>
        /// Gets the level (also known as Grade).
        /// </summary>
        /// <value>
        /// The level.
        /// </value>
        public CertificateGrade Level { get; }

        /// <summary>
        /// Gets the certificate.
        /// </summary>
        /// <value>
        /// The certificate.
        /// </value>
        public Certificate Certificate { get; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public CertificateStatus Status { get; private set; }

        /// <summary>                                                                                                             
        /// Gets the immediate prerequisite skills.                                                                                                            
        /// </summary>
        public IEnumerable<SkillLevel> PrerequisiteSkills { get; }

        /// <summary>
        /// Gets true whether the certificate is trained.
        /// </summary>
        /// <value>
        /// <c>true</c> if this certificate is trained; otherwise, <c>false</c>.
        /// </value>
        public bool IsTrained => Status == CertificateStatus.Trained;

        /// <summary>
        /// Gets true whether the certificate is partially trained.
        /// </summary>
        /// <value>
        /// <c>true</c> if this certificate is partilly trained; otherwise, <c>false</c>.
        /// </value>
        public bool IsPartiallyTrained => Status == CertificateStatus.PartiallyTrained;

        /// <summary>
        /// Gets the required training time for the provided character to train this certificate.
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetTrainingTime => m_character?.GetTrainingTimeToMultipleSkills(PrerequisiteSkills) ?? TimeSpan.Zero;

        /// <summary>
        /// Try to update the certificate's status. 
        /// </summary>
        /// <returns>True if the status was updated, false otherwise.</returns>
        internal bool TryUpdateCertificateStatus()
        {
            if (m_initialized)
                return false;

            bool noPrereq = true;
            bool trained = true;

            // Scan prerequisite skills
            foreach (SkillLevel prereqSkill in PrerequisiteSkills)
            {
                // Trained only if the skill's level is greater or equal than the minimum level
                trained &= prereqSkill.Skill.Level >= prereqSkill.Level;

                // Untrainable if no prereq is satisfied
                noPrereq &= prereqSkill.AllDependencies.All(x => !x.IsTrained);
            }

            // Updates status
            if (trained)
                Status = CertificateStatus.Trained;
            else if (noPrereq)
                Status = CertificateStatus.Untrained;
            else
                Status = CertificateStatus.PartiallyTrained;

            m_initialized = true;
            return true;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"Level {Skill.GetRomanFromInt((int)Level)}";
    }
}
