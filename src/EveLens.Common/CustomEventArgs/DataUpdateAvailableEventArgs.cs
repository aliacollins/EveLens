// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.ObjectModel;
using EveLens.Common.Serialization.PatchXml;

namespace EveLens.Common.CustomEventArgs
{
    public sealed class DataUpdateAvailableEventArgs : EventArgs
    {
        private readonly Collection<SerializableDatafile> m_changedFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataUpdateAvailableEventArgs"/> class.
        /// </summary>
        /// <param name="changedFiles">The changed files.</param>
        public DataUpdateAvailableEventArgs(Collection<SerializableDatafile> changedFiles)
        {
            m_changedFiles = changedFiles;
        }

        /// <summary>
        /// Gets or sets the changed files.
        /// </summary>
        /// <value>The changed files.</value>
        public Collection<SerializableDatafile> ChangedFiles => m_changedFiles;
    }
}