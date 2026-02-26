// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EveLens.Common.Extensions;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a mastery certificate.
    /// </summary>
    public sealed class MasteryCertificate
    {

        #region Constructor

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="masteryLevel">The mastery level.</param>
        /// <param name="src">The source.</param>
        internal MasteryCertificate(Mastery masteryLevel, SerializableMasteryCertificate src)
        {
            MasteryLevel = masteryLevel;
            Certificate = StaticCertificates.GetCertificateByID(src.ID);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MasteryCertificate"/> class.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="masteryCertificate">The mastery certificate.</param>
        internal MasteryCertificate(Character character, MasteryCertificate masteryCertificate)
        {
            if (masteryCertificate == null)
                return;

            MasteryLevel = masteryCertificate.MasteryLevel;
            Certificate = masteryCertificate.ToCharacter(character);
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the mastery level.
        /// </summary>
        public Mastery MasteryLevel { get; }

        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        public StaticCertificate Certificate { get; }

        /// <summary>
        /// Gets this certificate's representation for the provided character.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">character</exception>
        public Certificate ToCharacter(Character character)
        {
            character.ThrowIfNull(nameof(character));

            return character.Certificates.FirstOrDefault(x => x.ID == Certificate.ID);
        }

        #endregion

    }
}