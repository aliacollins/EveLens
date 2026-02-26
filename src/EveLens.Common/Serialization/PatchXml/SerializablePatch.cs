// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EveLens.Common.Data;

namespace EveLens.Common.Serialization.PatchXml
{
    [XmlRoot("evelens")]
    public sealed class SerializablePatch
    {
        private readonly Collection<SerializableRelease> m_releases;
        private readonly Collection<SerializableDatafile> m_datafiles;
        private readonly Collection<SerializableDatafile> m_changedDatafiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializablePatch"/> class.
        /// </summary>
        public SerializablePatch()
        {
            Release = new SerializableRelease();
            m_releases = new Collection<SerializableRelease>();
            m_datafiles = new Collection<SerializableDatafile>();
            m_changedDatafiles = new Collection<SerializableDatafile>();
        }

        /// <summary>
        /// Gets or sets the release.
        /// </summary>
        /// <value>
        /// The release.
        /// </value>
        /// <remarks> This xml element is to be used only for version 2</remarks>>
        [XmlElement("newest")]
        public SerializableRelease Release { get; set; }

        /// <summary>
        /// Gets or sets the releases.
        /// </summary>
        /// <value>
        /// The releases.
        /// </value>
        /// <remarks> This xml element is used for version 3+</remarks>>
        [XmlArray("releases")]
        [XmlArrayItem("release")]
        public Collection<SerializableRelease> Releases => m_releases;

        /// <summary>
        /// Gets the datafiles.
        /// </summary>
        /// <value>
        /// The datafiles.
        /// </value>
        [XmlArray("datafiles")]
        [XmlArrayItem("datafile")]
        public Collection<SerializableDatafile> Datafiles => m_datafiles;

        /// <summary>
        /// Gets the changed datafiles.
        /// </summary>
        /// <value>
        /// The changed datafiles.
        /// </value>
        [XmlIgnore]
        internal Collection<SerializableDatafile> ChangedDatafiles => m_changedDatafiles;

        /// <summary>
        /// Gets a value indicating whether files have changed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if files have changed; otherwise, <c>false</c>.
        /// </value>
        [XmlIgnore]
        internal bool FilesHaveChanged
        {
            get
            {
                m_changedDatafiles.Clear();

                foreach (Datafile datafile in EveLensClient.Datafiles)
                {
                    foreach (SerializableDatafile dfv in Datafiles.Where(dfv => dfv.Name == datafile.Filename))
                    {
                        if (datafile.MD5Sum != dfv.MD5Sum)
                            m_changedDatafiles.Add(dfv);

                        break;
                    }
                }

                return ChangedDatafiles.Count > 0;
            }
        }
    }
}