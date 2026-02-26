// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EveLens.Common.Attributes;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a certificate class from a character's point of view.
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class CertificateClass
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character">The character</param>
        /// <param name="src">The static certificate class</param>
        /// <param name="category">The owning category</param>
        internal CertificateClass(Character character, StaticCertificateClass src, CertificateGroup category)
        {
            Character = character;
            Category = category;
            StaticData = src;
            Certificate = new Certificate(character, src.Certificate, this);
        }

        /// <summary>
        /// Gets the character this certificate is bound to.
        /// </summary>
        public Character Character { get; }

        /// <summary>
        /// Gets the static data associated with this object.
        /// </summary>
        public StaticCertificateClass StaticData { get; }

        /// <summary>
        /// Gets the category for this certificate class.
        /// </summary>
        public CertificateGroup Category { get; private set; }

        /// <summary>
        /// Gets the certificate of this certificate class
        /// </summary>
        public Certificate Certificate { get; }

        /// <summary>
        /// Gets this skill's id.
        /// </summary>
        public int ID => StaticData.ID;

        /// <summary>
        /// Gets this skill's name.
        /// </summary>
        public string Name => StaticData.Name;

        /// <summary>
        /// Gets the lowest untrained certificate level.
        /// Null if all certificates have been trained.
        /// </summary>
        public CertificateLevel LowestUntrainedLevel
            => Certificate.AllLevel.FirstOrDefault(level => !level.IsTrained);

        /// <summary>
        /// Gets the highest trained certificate level.
        /// May be null if no level has been trained.
        /// </summary>
        public CertificateLevel HighestTrainedLevel
            => Certificate.AllLevel.LastOrDefault(level => level.IsTrained);

        /// <summary>
        /// Gets true if the provided character has completed this class.
        /// </summary>
        public bool IsCompleted => Certificate.AllLevel.All(certLevel => certLevel.IsTrained);

        /// <summary>
        /// Gets true if the provided character can train to the next grade,
        /// false if the class has already been completed or if the next grade is untrainable.
        /// </summary>
        /// <returns></returns>
        public bool IsFurtherTrainable
        {
            get
            {
                if (Certificate.AllLevel.All(cert => cert.IsTrained))
                    return false;

                foreach (CertificateLevel certLevel in Certificate.AllLevel)
                {
                    switch (certLevel.Status)
                    {
                        case CertificateStatus.PartiallyTrained:
                            return true;
                        case CertificateStatus.Untrained:
                            return false;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => StaticData.Name;
    }
}