// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using System.Text;
using EVEMon.Common.Extensions;
using EVEMon.Common.Serialization.Datafiles;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Represents an item category
    /// </summary>
    public class MarketGroup
    {
        #region Constructors

        /// <summary>
        /// Deserialization constructor for root category only.
        /// </summary>
        /// <param name="src">Source Serializable Market Group</param>
        private MarketGroup(SerializableMarketGroup src)
        {
            ID = src.ID;
            Name = src.Name;
            SubGroups = new MarketGroupCollection(this, src.SubGroups);
            Items = new ItemCollection(this, src.Items);
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="parent">The Market Group this Market Group is contained within</param>
        /// <param name="src">Source Serializable Market Group</param>
        public MarketGroup(MarketGroup parent, SerializableMarketGroup src)
            : this(src)
        {
            ParentGroup = parent;
        }

        /// <summary>
        /// Deserialization constructor for root category only.
        /// </summary>
        /// <param name="src"></param>
        /// <exception cref="System.ArgumentNullException">src</exception>
        protected MarketGroup(SerializableBlueprintMarketGroup src)
        {
            src.ThrowIfNull(nameof(src));

            ID = src.ID;
            Name = src.Name;
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="parent">The Market Group this Market Group is contained within</param>
        /// <param name="src">Source Blueprint Group</param>
        protected MarketGroup(MarketGroup parent, SerializableBlueprintMarketGroup src)
            : this(src)
        {
            ParentGroup = parent;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the group ID.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets the parent category. <c>Null</c> for the root category.
        /// </summary>
        public MarketGroup ParentGroup { get; }

        /// <summary>
        /// Gets the sub categories.
        /// </summary>
        public MarketGroupCollection SubGroups { get; }

        /// <summary>
        /// Gets the items in this category.
        /// </summary>
        public ItemCollection Items { get; }

        /// <summary>
        /// Gets this category's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the collection of all the items in this category and its descendants.
        /// </summary>
        public IEnumerable<Item> AllItems
        {
            get
            {
                foreach (Item item in Items)
                {
                    yield return item;
                }

                foreach (Item subItem in SubGroups.SelectMany(cat => cat.AllItems))
                {
                    yield return subItem;
                }
            }
        }

        /// <summary>
        /// Gets this category full category name.
        /// </summary>
        public string CategoryPath
        {
            get
            {
                StringBuilder fullCategoryPath = new StringBuilder();
                MarketGroup group = this;

                while (group != null)
                {
                    fullCategoryPath.Insert(0, group.Name);
                    group = group.ParentGroup;
                    if (group != null)
                        fullCategoryPath.Insert(0, " > ");
                }

                return fullCategoryPath.ToString();
            }
        }

        #endregion


        #region Overidden Methods

        /// <summary>
        /// Gets the name of this item.
        /// </summary>
        /// <returns>Name of the Market Group.</returns>
        public override string ToString() => Name;

        #endregion
    }
}