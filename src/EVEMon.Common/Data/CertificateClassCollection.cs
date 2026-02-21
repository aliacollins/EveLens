// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EVEMon.Common.Collections;
using EVEMon.Common.Models;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Represetns a collection of character's certificate class
    /// </summary>
    public sealed class CertificateClassCollection : ReadonlyKeyedCollection<int, CertificateClass>
    {
        private static CertificateClassCollection s_certificatesCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateClassCollection"/> class.
        /// Used to build a non-character associated certificates collection.
        /// </summary>
        public CertificateClassCollection()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateClassCollection"/> class.
        /// Used to build a character associated certificates collection.
        /// </summary>
        /// <param name="character">The character.</param>
        internal CertificateClassCollection(Character character)
        {
            var source = character?.CertificateCategories.SelectMany(category => category)
                      ?? StaticCertificates.AllGroups?.SelectMany(group => new CertificateGroup(group));

            if (source == null)
                return;

            foreach (CertificateClass certClass in source)
            {
                Items[certClass.ID] = certClass;
            }
        }

        /// <summary>
        /// Gets a certificate class from its name.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public CertificateClass this[int id] => GetByKey(id);

        /// <summary>
        /// Gets a collection of non-character assosiated certificates.
        /// </summary>
        /// <value>
        /// The skills.
        /// </value>
        public static CertificateClassCollection CertificateClasses
            => s_certificatesCollection ?? (s_certificatesCollection = new CertificateClassCollection());
    }
}