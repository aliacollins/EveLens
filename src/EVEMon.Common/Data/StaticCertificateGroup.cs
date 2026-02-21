// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Collections;
using EVEMon.Common.Serialization.Datafiles;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Represents a certificate group.
    /// </summary>
    public sealed class StaticCertificateGroup : ReadonlyCollection<StaticCertificateClass>
    {
        #region Constructor

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="src"></param>
        internal StaticCertificateGroup(SerializableCertificateGroup src)
            : base(src.Classes.Count)
        {
            ID = src.ID;
            Name = src.Name;
            Description = src.Description;

            foreach (SerializableCertificateClass srcClass in src.Classes)
            {
                Items.Add(new StaticCertificateClass(this, srcClass));
            }
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets this group's id.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets this group's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets this group's description.
        /// </summary>
        public string Description { get; }

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