// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common.Controls;
using EVEMon.Common.Services;

namespace EVEMon.SettingsUI
{
    public partial class EsiScopeEditorForm : EVEMonForm
    {
        private bool m_isUpdating;

        /// <summary>
        /// The scopes selected by the user when the dialog closes with OK.
        /// </summary>
        public HashSet<string> SelectedScopes { get; private set; }

        /// <summary>
        /// The detected preset name for the current selection.
        /// </summary>
        public string SelectedPreset { get; private set; }

        private EsiScopeEditorForm()
        {
            InitializeComponent();
            SelectedScopes = new HashSet<string>();
            SelectedPreset = EsiScopePresets.FullMonitoring;
        }

        /// <summary>
        /// Creates the scope editor with the given set of currently selected scopes.
        /// </summary>
        public EsiScopeEditorForm(HashSet<string> currentScopes) : this()
        {
            SelectedScopes = new HashSet<string>(currentScopes);
            PopulateTree();
            ApplyChecks(currentScopes);
        }

        /// <summary>
        /// Populates the tree with feature groups and their scopes.
        /// </summary>
        private void PopulateTree()
        {
            scopeTreeView.BeginUpdate();
            scopeTreeView.Nodes.Clear();

            foreach (var group in EsiScopePresets.FeatureGroups)
            {
                var groupNode = new TreeNode(group.Name) { Tag = "group" };
                foreach (string scope in group.Scopes)
                {
                    var scopeNode = new TreeNode(scope) { Tag = scope };
                    groupNode.Nodes.Add(scopeNode);
                }
                scopeTreeView.Nodes.Add(groupNode);
            }

            scopeTreeView.ExpandAll();
            scopeTreeView.EndUpdate();
        }

        /// <summary>
        /// Applies the given set of scopes as checked items in the tree.
        /// </summary>
        private void ApplyChecks(HashSet<string> scopes)
        {
            m_isUpdating = true;

            foreach (TreeNode groupNode in scopeTreeView.Nodes)
            {
                bool allChecked = true;
                foreach (TreeNode scopeNode in groupNode.Nodes)
                {
                    bool isChecked = scopes.Contains((string)scopeNode.Tag);
                    scopeNode.Checked = isChecked;
                    if (!isChecked)
                        allChecked = false;
                }
                groupNode.Checked = allChecked && groupNode.Nodes.Count > 0;
            }

            m_isUpdating = false;
        }

        /// <summary>
        /// Handles check propagation: parent toggles all children,
        /// children update parent state.
        /// </summary>
        private void scopeTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (m_isUpdating)
                return;

            m_isUpdating = true;

            TreeNode node = e.Node!;

            // If a group node was toggled, propagate to children
            if (node.Tag is string tag && tag == "group")
            {
                foreach (TreeNode child in node.Nodes)
                    child.Checked = node.Checked;
            }
            else
            {
                // A scope node changed - update parent
                TreeNode parent = node.Parent;
                if (parent != null)
                {
                    bool allChecked = true;
                    foreach (TreeNode sibling in parent.Nodes)
                    {
                        if (!sibling.Checked)
                        {
                            allChecked = false;
                            break;
                        }
                    }
                    parent.Checked = allChecked;
                }
            }

            // Enforce mandatory Skills & Training Queue
            var skillsNode = scopeTreeView.Nodes.Cast<TreeNode>()
                .FirstOrDefault(n => n.Text == "Skills & Training Queue");
            if (skillsNode != null && !skillsNode.Checked)
            {
                bool anyChildChecked = false;
                foreach (TreeNode child in skillsNode.Nodes)
                {
                    if (child.Checked)
                    {
                        anyChildChecked = true;
                        break;
                    }
                }

                if (!anyChildChecked)
                {
                    // Re-enable skills as mandatory
                    foreach (TreeNode child in skillsNode.Nodes)
                        child.Checked = true;
                    skillsNode.Checked = true;

                    m_isUpdating = false;
                    MessageBox.Show(
                        "Skills & Training Queue scopes are required for EVEMon to function.",
                        "Required Scopes",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            m_isUpdating = false;
        }

        /// <summary>
        /// Collects currently checked scopes from the tree.
        /// </summary>
        private HashSet<string> CollectCheckedScopes()
        {
            var scopes = new HashSet<string>();
            foreach (TreeNode groupNode in scopeTreeView.Nodes)
            {
                foreach (TreeNode scopeNode in groupNode.Nodes)
                {
                    if (scopeNode.Checked && scopeNode.Tag is string scope)
                        scopes.Add(scope);
                }
            }
            return scopes;
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            ApplyChecks(new HashSet<string>(EsiScopePresets.AllScopes));
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            // Clear all, but the AfterCheck handler will re-enable Skills & Training Queue
            var empty = new HashSet<string>();
            ApplyChecks(empty);

            // Skills are mandatory - re-enable
            var skillsScopes = EsiScopePresets.FeatureGroups
                .First(g => g.Name == "Skills & Training Queue").Scopes;
            var mandatory = new HashSet<string>(skillsScopes);
            ApplyChecks(mandatory);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedScopes = CollectCheckedScopes();
            SelectedPreset = EsiScopePresets.DetectPreset(SelectedScopes);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
