// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Data;
using EVEMon.Common.Models;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// Reusable tree node model that preserves the full MarketGroup hierarchy.
    /// Used by Ship Browser, Item Browser, and Blueprint Browser to display
    /// multi-level trees instead of flat group lists.
    /// </summary>
    public sealed class BrowserTreeNode
    {
        /// <summary>Display name of this node.</summary>
        public string Name { get; }

        /// <summary>0 = root child, 1 = sub-category, etc.</summary>
        public int Depth { get; }

        /// <summary>True if this represents an Item, false if a category node.</summary>
        public bool IsLeaf { get; }

        /// <summary>The item (only set when IsLeaf=true).</summary>
        public Item? Item { get; }

        /// <summary>Whether the character can use this item (only meaningful when IsLeaf=true).</summary>
        public bool CanUse { get; }

        /// <summary>The market group (only set when IsLeaf=false).</summary>
        public MarketGroup? MarketGroup { get; }

        /// <summary>All direct children (both category and leaf nodes).</summary>
        public List<BrowserTreeNode> Children { get; }

        /// <summary>Children visible after filtering.</summary>
        public List<BrowserTreeNode> VisibleChildren { get; private set; }

        /// <summary>Whether this category node is expanded in the tree.</summary>
        public bool IsExpanded { get; set; }

        /// <summary>Total number of leaf items in this subtree.</summary>
        public int TotalLeafCount { get; }

        /// <summary>Creates a category node.</summary>
        private BrowserTreeNode(string name, int depth, MarketGroup group, List<BrowserTreeNode> children)
        {
            Name = name;
            Depth = depth;
            IsLeaf = false;
            MarketGroup = group;
            Children = children;
            VisibleChildren = children;
            IsExpanded = depth == 0;
            TotalLeafCount = children.Sum(c => c.TotalLeafCount);
        }

        /// <summary>Creates a leaf (item) node.</summary>
        private BrowserTreeNode(string name, int depth, Item item, bool canUse)
        {
            Name = name;
            Depth = depth;
            IsLeaf = true;
            Item = item;
            CanUse = canUse;
            Children = new List<BrowserTreeNode>();
            VisibleChildren = new List<BrowserTreeNode>();
            TotalLeafCount = 1;
        }

        /// <summary>
        /// Builds a tree from a MarketGroup, recursively creating nodes for sub-groups and items.
        /// </summary>
        public static BrowserTreeNode FromMarketGroup(MarketGroup group, int depth, Character? character)
        {
            var children = new List<BrowserTreeNode>();

            // Add sub-groups first (sorted by name)
            foreach (var subGroup in group.SubGroups.OrderBy(g => g.Name))
            {
                var child = FromMarketGroup(subGroup, depth + 1, character);
                if (child.TotalLeafCount > 0)
                    children.Add(child);
            }

            // Add items as leaf nodes (sorted by name)
            foreach (var item in group.Items.OrderBy(i => i.Name))
            {
                bool canUse = CanCharacterUse(item, character);
                children.Add(new BrowserTreeNode(item.Name, depth + 1, item, canUse));
            }

            return new BrowserTreeNode(group.Name, depth, group, children);
        }

        /// <summary>
        /// Applies a text filter and optional can-use filter.
        /// Returns true if this node or any descendant is visible.
        /// </summary>
        public bool ApplyFilter(string textFilter, bool canUseOnly)
        {
            if (IsLeaf)
            {
                bool visible = true;

                if (canUseOnly && !CanUse)
                    visible = false;

                if (visible && !string.IsNullOrEmpty(textFilter))
                    visible = Name.Contains(textFilter, StringComparison.OrdinalIgnoreCase);

                return visible;
            }

            // Category node: filter children recursively
            var visible2 = new List<BrowserTreeNode>();
            foreach (var child in Children)
            {
                if (child.ApplyFilter(textFilter, canUseOnly))
                    visible2.Add(child);
            }

            VisibleChildren = visible2;
            return visible2.Count > 0;
        }

        /// <summary>
        /// Produces a flat list by walking the tree, only descending into expanded category nodes.
        /// The view binds a single ItemsControl to this flat list.
        /// </summary>
        public static List<BrowserTreeNode> FlattenVisible(IEnumerable<BrowserTreeNode> roots)
        {
            var result = new List<BrowserTreeNode>();
            foreach (var root in roots)
            {
                FlattenNode(root, result);
            }
            return result;
        }

        private static void FlattenNode(BrowserTreeNode node, List<BrowserTreeNode> result)
        {
            result.Add(node);
            if (!node.IsLeaf && node.IsExpanded)
            {
                foreach (var child in node.VisibleChildren)
                {
                    FlattenNode(child, result);
                }
            }
        }

        /// <summary>
        /// Resets the filter so all children are visible.
        /// </summary>
        public void ClearFilter()
        {
            VisibleChildren = Children;
            foreach (var child in Children)
            {
                if (!child.IsLeaf)
                    child.ClearFilter();
            }
        }

        /// <summary>
        /// Sets IsExpanded on all category nodes recursively.
        /// </summary>
        public void SetExpandedAll(bool expanded)
        {
            if (!IsLeaf)
            {
                IsExpanded = expanded;
                foreach (var child in Children)
                    child.SetExpandedAll(expanded);
            }
        }

        private static bool CanCharacterUse(Item item, Character? character)
        {
            if (character == null)
                return false;

            foreach (var prereq in item.Prerequisites)
            {
                if (character.GetSkillLevel(prereq.Skill) < prereq.Level)
                    return false;
            }

            return true;
        }
    }
}
