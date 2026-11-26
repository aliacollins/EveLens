// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Constants;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Splits five attribute values (implants already subtracted) into the remappable
    /// allocation and a temporary bonus that is equal on every attribute.
    /// Values that do not fit that shape are returned unchanged with a zero bonus.
    /// </summary>
    public sealed class AttributeDecomposition
    {
        private const int AttributeCount = 5;

        private const long MinAllocation = EveConstants.CharacterBaseAttributePoints;

        private const long MaxAllocation = EveConstants.CharacterBaseAttributePoints
            + EveConstants.MaxRemappablePointsPerAttribute;

        private const long AllocationTotal = AttributeCount * EveConstants.CharacterBaseAttributePoints
            + EveConstants.SpareAttributePointsOnRemap;

        private AttributeDecomposition(long[] allocation, long bonus, bool supported)
        {
            Allocation = Array.AsReadOnly(allocation);
            TemporaryBonus = bonus;
            IsSupported = supported;
        }

        /// <summary>The remappable points per attribute, in the order they were given.</summary>
        public IReadOnlyList<long> Allocation { get; }

        /// <summary>The bonus every attribute carries on top of its allocation.</summary>
        public long TemporaryBonus { get; }

        /// <summary>True when the values were a legal allocation plus a uniform bonus.</summary>
        public bool IsSupported { get; }

        /// <summary>
        /// Splits the given values. A legal allocation totals <see cref="AllocationTotal"/>
        /// with every attribute between <see cref="MinAllocation"/> and <see cref="MaxAllocation"/>.
        /// </summary>
        /// <param name="withoutImplants">Exactly five attribute values with implant bonuses removed.</param>
        public static AttributeDecomposition FromObserved(IEnumerable<long> withoutImplants)
        {
            long[] values = withoutImplants.ToArray();
            if (values.Length != AttributeCount)
                throw new ArgumentException("Exactly five attributes are required.", nameof(withoutImplants));

            // The range check also keeps the sum below from overflowing.
            if (values.Any(v => v < 0 || v > int.MaxValue))
                return new AttributeDecomposition(values, 0, false);

            long extra = values.Sum() - AllocationTotal;
            if (extra < 0 || extra % AttributeCount != 0)
                return new AttributeDecomposition(values, 0, false);

            long bonus = extra / AttributeCount;
            long[] allocation = values.Select(v => v - bonus).ToArray();
            return allocation.All(v => v >= MinAllocation && v <= MaxAllocation)
                ? new AttributeDecomposition(allocation, bonus, true)
                : new AttributeDecomposition(values, 0, false);
        }
    }
}
