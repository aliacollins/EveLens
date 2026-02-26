// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using EveLens.Common.Helpers;
using EveLens.Common.Serialization.Eve;
using EveLens.Core;
using System;
using System.Threading.Tasks;

namespace EveLens.Common.Models
{
    public sealed class EmploymentRecord
    {
        public event EventHandler EmploymentRecordImageUpdated;


        #region Fields

        private readonly Character m_character;
        private readonly long m_corporationId;

        private string m_corporationName;
        private object? m_image;

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor from the API.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="src">The source.</param>
        /// <exception cref="System.ArgumentNullException">src</exception>
        public EmploymentRecord(Character character, SerializableEmploymentHistoryListItem src)
        {
            src.ThrowIfNull(nameof(src));

            m_character = character;
            m_corporationId = src.CorporationID;
            m_corporationName = string.IsNullOrWhiteSpace(src.CorporationName)
                ? ServiceLocator.NameResolver.GetName(src.CorporationID) : src.CorporationName;
            StartDate = src.StartDate;
        }

        /// <summary>
        /// Constructor from the settings.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="src">The source.</param>
        /// <exception cref="System.ArgumentNullException">src</exception>
        public EmploymentRecord(Character character, SerializableEmploymentHistory src)
        {
            src.ThrowIfNull(nameof(src));

            m_character = character;
            m_corporationId = src.CorporationID;
            m_corporationName = src.CorporationName;
            StartDate = src.StartDate;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the corporation.
        /// </summary>
        /// <value>The name of the corporation.</value>
        public string CorporationName => m_corporationName.IsEmptyOrUnknown() ?
            (m_corporationName = ServiceLocator.NameResolver.GetName(m_corporationId)) : m_corporationName;

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public DateTime StartDate { get; }

        /// <summary>
        /// Gets the corporation image.
        /// </summary>
        /// <value>The corporation image.</value>
        public object? CorporationImage
        {
            get
            {
                if (m_image != null)
                    return m_image;

                GetImageAsync().ConfigureAwait(false);

                return m_image ?? (m_image = Properties.Resources.DefaultCorporationImage32);
            }
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Gets the corporation image.
        /// </summary>
        private async Task GetImageAsync()
        {
            Uri uri = ImageHelper.GetCorporationImageURL(m_corporationId);
            object? img = await ServiceLocator.ImageService.GetImageAsync(uri).ConfigureAwait(false);
            if (img != null)
            {
                m_image = img;
                EmploymentRecordImageUpdated?.ThreadSafeInvoke(this, EventArgs.Empty);
            }
        }

        #endregion


        #region Export Method

        /// <summary>
        /// Exports the given object to a serialization object.
        /// </summary>
        public SerializableEmploymentHistory Export()
        {
            SerializableEmploymentHistory serial = new SerializableEmploymentHistory
            {
                CorporationID = m_corporationId,
                CorporationName = CorporationName,
                StartDate = StartDate
            };
            return serial;
        }

        #endregion

    }
}
