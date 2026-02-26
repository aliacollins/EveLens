// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EveLens.Common.Extensions;
using EveLens.Common.Helpers;
using EveLens.Common.Serialization.Esi;
using EveLens.Core;

namespace EveLens.Common.Models
{
    public sealed class Loyalty : IComparable<Loyalty>
    {
        public event EventHandler LoyaltyCorpImageUpdated;

        #region Fields

        private readonly Character m_character;

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
        internal Loyalty(Character character, EsiLoyaltyListItem src)
        {
            m_character = character;

            LoyaltyPoints = src.LoyaltyPoints;
            CorpId = src.CorpID;
            m_corporationName = ServiceLocator.NameResolver.GetName(src.CorpID);
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the corporation.
        /// </summary>
        /// <value>The name of the corporation.</value>
        public string CorporationName => m_corporationName.IsEmptyOrUnknown() ?
            (m_corporationName = ServiceLocator.NameResolver.GetName(CorpId)) : m_corporationName;

        /// <summary>
        /// Gets or sets the loyalty point value.
        /// </summary>
        /// <value>The loyalty point value.</value>
        public int LoyaltyPoints { get; }

        /// <summary>
        /// Gets or sets the corp ID.
        /// </summary>
        /// <value>The corp ID.</value>
        public int CorpId { get; }

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
            Uri uri = ImageHelper.GetCorporationImageURL(CorpId);
            object? img = await ServiceLocator.ImageService.GetImageAsync(uri).ConfigureAwait(false);
            if (img != null) {
                m_image = img;
                LoyaltyCorpImageUpdated?.ThreadSafeInvoke(this, EventArgs.Empty);
            }
        }

        public int CompareTo(Loyalty other)
        {
            // Descending order of LP earned
            return other.LoyaltyPoints.CompareTo(LoyaltyPoints);
        }

        #endregion
    }
}
