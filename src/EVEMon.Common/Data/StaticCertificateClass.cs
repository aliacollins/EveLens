// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Serialization.Datafiles;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Represents a certificate class.
    /// </summary>
    public sealed class StaticCertificateClass
    {
        #region Constructor

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="src"></param>
        internal StaticCertificateClass(StaticCertificateGroup group, SerializableCertificateClass src)
        {
            ID = src.ID;
            Name = src.Name;
            Description = src.Description;
            Group = group;
            Certificate = new StaticCertificate(this, src.Certificate);
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets this class's id.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets this class's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets this class's description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the certificates class's Group.
        /// </summary>
        public StaticCertificateGroup Group { get; }

        public StaticCertificate Certificate { get; }

        #endregion

        
        #region Overridden Methods

        /// <summary>
        /// Gets a string representation of this class.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Name;

        #endregion
    }
}