// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Collections;
using EveLens.Common.Serialization.Eve;

namespace EveLens.Common.Models.Collections
{
    public sealed class EmploymentRecordCollection : ReadonlyCollection<EmploymentRecord>
    {
        private readonly Character m_character;


        #region Constructor

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        internal EmploymentRecordCollection(Character character)
        {
            m_character = character;
        }

        #endregion


        #region Import & Export Methods

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable objects from the API.</param>
        internal void Import(IEnumerable<SerializableEmploymentHistoryListItem> src)
        {
            Items.Clear();

            // Import the employment history from the API
            foreach (SerializableEmploymentHistoryListItem srcEmploymentRecord in src)
            {
                Items.Add(new EmploymentRecord(m_character, srcEmploymentRecord));
            }
        }

        /// <summary>
        /// Imports an enumeration of serialization objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable objects from the settings file.</param>
        internal void Import(IEnumerable<SerializableEmploymentHistory> src)
        {
            Items.Clear();

            foreach (SerializableEmploymentHistory srcEmploymentRecord in src)
            {
                Items.Add(new EmploymentRecord(m_character, srcEmploymentRecord));
            }
        }

        /// <summary>
        /// Exports the serialization object for the settings file.
        /// </summary>
        /// <returns>List of serializable employment records.</returns>
        internal IEnumerable<SerializableEmploymentHistory> Export()
            => Items.Select(employmentRecord => employmentRecord.Export());

        #endregion
    }
}