// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Constants;
using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Describes a property of a ship/item (e.g. CPU size)
    /// </summary>
    public struct EvePropertyValue
    {
        #region Constructor

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="src"></param>
        internal EvePropertyValue(SerializablePropertyValue src)
            : this()
        {
            Property = StaticProperties.GetPropertyByID(src.ID);
            Value = src.Value;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the property.
        /// </summary>
        public EveProperty Property { get; }

        /// <summary>
        /// Gets the property value.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the integer value.
        /// </summary>
        public long Int64Value => long.Parse(Value, CultureConstants.InvariantCulture);

        /// <summary>
        /// Gets the floating point value.
        /// </summary>
        public double DoubleValue => double.Parse(Value, CultureConstants.InvariantCulture);

        #endregion


        #region Overridden Methods

        /// <summary>
        /// Gets a string representation of this prerequisite.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Property.Name;

        #endregion
    }
}
