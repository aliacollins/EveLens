// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Collections;
using EVEMon.Common.Extensions;
using EVEMon.Common.Serialization.Datafiles;

namespace EVEMon.Common.Data
{
    public sealed class EvePropertyCategory : ReadonlyCollection<EveProperty>
    {
        #region Constructor

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="serial"></param>
        /// <exception cref="System.ArgumentNullException">serial</exception>
        public EvePropertyCategory(SerializablePropertyCategory serial)
            : base(serial?.Properties.Count ?? 0)
        {
            serial.ThrowIfNull(nameof(serial));

            ID = serial.ID;
            Name = serial.Name;
            Description = serial.Description;

            foreach (SerializableProperty serialProp in serial.Properties)
            {
                Items.Add(new EveProperty(this, serialProp));
            }

            // Sets the display name
            switch (Name)
            {
                default:
                    DisplayName = Name;
                    break;
                case "NULL":
                    DisplayName = "System";
                    break;
            }
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets this category's id.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets this category's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets this category's description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the category's display name.
        /// </summary>
        public string DisplayName { get; }

        #endregion
    }
}