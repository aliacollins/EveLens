// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Data;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel that groups Item.Properties by EvePropertyCategory for structured display
    /// in ship and item detail panels.
    /// </summary>
    public sealed class ItemPropertiesViewModel
    {
        private readonly List<PropertySection> _sections;

        /// <summary>
        /// Gets the property sections grouped by category.
        /// </summary>
        public IReadOnlyList<PropertySection> Sections => _sections;

        /// <summary>
        /// Creates a new ItemPropertiesViewModel for the given item.
        /// </summary>
        public ItemPropertiesViewModel(Item? item)
        {
            _sections = new List<PropertySection>();

            if (item?.Properties == null)
                return;

            var groups = new Dictionary<string, List<PropertyRow>>();

            foreach (var propValue in item.Properties)
            {
                var property = propValue.Property;
                if (property == null)
                    continue;

                string categoryName = property.Category?.DisplayName ?? "General";
                string formattedValue = property.GetLabelOrDefault(item);

                if (string.IsNullOrWhiteSpace(formattedValue))
                    continue;

                if (!groups.ContainsKey(categoryName))
                    groups[categoryName] = new List<PropertyRow>();

                groups[categoryName].Add(new PropertyRow(
                    property.Name,
                    formattedValue,
                    property.Unit ?? ""));
            }

            _sections = groups
                .OrderBy(g => g.Key)
                .Select(g => new PropertySection(
                    g.Key,
                    g.Value.OrderBy(p => p.Name).ToList()))
                .ToList();
        }
    }

    /// <summary>
    /// A group of item properties under a single category.
    /// </summary>
    public sealed class PropertySection
    {
        public string CategoryName { get; }
        public IReadOnlyList<PropertyRow> Properties { get; }

        public PropertySection(string categoryName, List<PropertyRow> properties)
        {
            CategoryName = categoryName;
            Properties = properties;
        }
    }

    /// <summary>
    /// A single property name-value pair for display.
    /// </summary>
    public sealed class PropertyRow
    {
        public string Name { get; }
        public string FormattedValue { get; }
        public string Unit { get; }

        public PropertyRow(string name, string formattedValue, string unit)
        {
            Name = name;
            FormattedValue = formattedValue;
            Unit = unit;
        }
    }
}
