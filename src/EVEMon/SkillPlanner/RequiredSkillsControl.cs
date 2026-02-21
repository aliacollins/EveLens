// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Data;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Services;

namespace EVEMon.SkillPlanner
{
    /// <summary>
    /// User control to display required skills for a given eveobject and update a plan object for requirements not met
    /// </summary>
    public partial class RequiredSkillsControl : UserControl
    {
        private BlueprintActivity m_activity;
        private Item? m_object;
        private Plan? m_plan;
        private bool m_allExpanded;

        private IDisposable? _planChangedSub;


        #region Object Lifecycle

        /// <summary>
        /// Default constructor
        /// </summary>
        public RequiredSkillsControl()
        {
            InitializeComponent();

            tvSkillList.MouseDown += tvSkillList_MouseDown;
            tvSkillList.MouseMove += tvSkillList_MouseMove;

            Disposed += OnDisposed;
            _planChangedSub = AppServices.EventAggregator.SubscribeOnUI<PlanChangedEvent>(this, OnPlanChanged);
        }

        /// <summary>
        /// Unsubscribe events on disposing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDisposed(object? sender,EventArgs e)
        {
            Disposed -= OnDisposed;
            _planChangedSub?.Dispose();
        }

        /// <summary>
        /// Occurs when the plan changes, when update the display.
        /// </summary>
        private void OnPlanChanged(PlanChangedEvent e)
        {
            UpdateDisplay();
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets or sets the activity.
        /// </summary>
        /// <value>
        /// The activity.
        /// </value>
        internal BlueprintActivity Activity
        {
            get { return m_activity; }
            set
            {
                m_activity = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// An EveObject for which we want to show required skills
        /// </summary>
        internal Item? Object
        {
            get { return m_object; }
            set
            {
                m_object = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// The target Plan object to add any required skills
        /// </summary>
        internal Plan? Plan
        {
            get { return m_plan; }
            set
            {
                m_plan = value;
                UpdateDisplay();
            }
        }

        #endregion


        #region Content creation

        /// <summary>
        /// Updates control contents
        /// </summary>
        private void UpdateDisplay()
        {
            // We have nothing to display
            if (m_object == null || m_plan == null)
                return;

            // Default all known flag to true. Will be set to false in getSkillNode() if a requirement is not met
            bool allSkillsKnown = true;

            // Default unplanned skills flag to false. Will be set to true in getSkillNode() if a requirement is neither met nor planned
            bool skillsUnplanned = false;

            // Treeview update
            tvSkillList.BeginUpdate();

            IList<StaticSkillLevel> prerequisites = Activity == BlueprintActivity.None
                ? m_object.Prerequisites
                    .Where(x => !x.Level.Equals(0) && x.Activity.Equals(Activity)).ToList()
                : m_object.Prerequisites
                    .Where(x => !x.Level.Equals(0) && x.Activity.Equals(Activity))
                    .OrderBy(x => x.Skill.Name).ToList();

            try
            {
                tvSkillList.Nodes.Clear();

                // Recursively create nodes
                foreach (StaticSkillLevel prereq in prerequisites)
                {
                    tvSkillList.Nodes.Add(GetSkillNode(prereq, ref allSkillsKnown, ref skillsUnplanned));
                }
            }
            finally
            {
                tvSkillList.EndUpdate();
            }

            // Set training time required label
            if (allSkillsKnown)
                lblTimeRequired.Text = @"No training required";
            else
            {
                TimeSpan trainTime = m_plan.Character.GetTrainingTimeToMultipleSkills(prerequisites);
                lblTimeRequired.Text = trainTime.ToDescriptiveText(DescriptiveTextOptions.IncludeCommas);
            }

            // Set minimun control size
            Size timeRequiredTextSize = TextRenderer.MeasureText(lblTimeRequired.Text, Font);
            Size newMinimumSize = new Size(timeRequiredTextSize.Width + btnAddSkills.Width, 0);
            if (MinimumSize.Width < newMinimumSize.Width)
                MinimumSize = newMinimumSize;

            // Enable / disable button
            btnAddSkills.Enabled = skillsUnplanned;
        }

        /// <summary>
        /// Recursive method to generate treenodes for tvSkillList.
        /// </summary>
        /// <param name="prereq">The prereq.</param>
        /// <param name="allSkillsKnown">if set to <c>true</c> [all skills known].</param>
        /// <param name="skillsUnplanned">if set to <c>true</c> [skills unplanned].</param>
        /// <returns></returns>
        private TreeNode GetSkillNode(ISkillLevel prereq, ref bool allSkillsKnown, ref bool skillsUnplanned)
        {
            if (prereq.Skill == null)
                return new TreeNode();

            Character character = (Character)m_plan!.Character;
            Skill skill = character.Skills[prereq.Skill.ID];
            TreeNode node = new TreeNode(prereq.ToString()) { Tag = new SkillLevel(skill, prereq.Level) };

            // Skill requirement met
            if (skill.Level >= prereq.Level)
            {
                node.ImageIndex = 1;
                node.SelectedImageIndex = 1;
            }
                // Requirement not met, but planned
            else if (m_plan.IsPlanned(skill, prereq.Level))
            {
                node.ImageIndex = 2;
                node.SelectedImageIndex = 2;
                allSkillsKnown = false;
            }
                // Requirement not met, but trainable
            else if (skill.Level < prereq.Level && skill.IsKnown)
            {
                node.ImageIndex = 3;
                node.SelectedImageIndex = 3;
                allSkillsKnown = false;
                skillsUnplanned = true;
            }
                // Requirement not met
            else
            {
                node.ImageIndex = 0;
                node.SelectedImageIndex = 0;
                allSkillsKnown = false;
                skillsUnplanned = true;
            }

            // Generate child nodes if required
            foreach (StaticSkillLevel childPrereq in skill.StaticData.Prerequisites.Where(childPrereq => childPrereq != prereq))
            {
                node.Nodes.Add(GetSkillNode(childPrereq, ref allSkillsKnown, ref skillsUnplanned));
            }

            return node;
        }

        #endregion


        #region Event Handlers

        /// <summary>
        /// When the treeview is clicked, we manually select nodes since the bounding boxes are incorrect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvSkillList_MouseDown(object? sender,MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                tvSkillList.Cursor = Cursors.Default;

            // Perform the selection manually since the bound's width and x are incorrect
            TreeNode? selection = null;
            for (TreeNode? node = tvSkillList.TopNode; node != null; node = node.NextVisibleNode)
            {
                if (node.Bounds.Top > e.Y || node.Bounds.Bottom < e.Y)
                    continue;

                // If the user clicked the "arrow zone", we do not change the selection and just return
                if (e.X < node.Bounds.Left - 32)
                    return;

                selection = node;
                break;
            }

            tvSkillList.SelectedNode = selection;
        }

        /// <summary>
        /// When the mouse moves over the list, we change the cursor.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void tvSkillList_MouseMove(object? sender,MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                return;

            tvSkillList.Cursor = CustomCursors.ContextMenu;
        }

        /// <summary>
        /// Event handler method for Add Skills button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddSkills_Click(object? sender,EventArgs e)
        {
            // Add skills to plan
            IPlanOperation? operation = m_plan!.TryAddSet(m_object!.Prerequisites.Where(x => x.Activity.Equals(Activity)),
                                                        m_object.Name);
            if (operation == null)
                return;

            PlanWindow? planWindow = ParentForm as PlanWindow;
            if (planWindow == null)
                return;

            PlanHelper.Perform(new PlanToOperationWindow(operation), planWindow);

            // Refresh display to reflect plan changes
            UpdateDisplay();
        }

        /// <summary>
        /// Event handler for treenode double click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvSkillList_NodeMouseDoubleClick(object? sender,TreeNodeMouseClickEventArgs e)
        {
            Skill? skill = ((SkillLevel)e.Node.Tag)?.Skill;

            // Open skill browser tab for this skill
            PlanWindow.ShowPlanWindow(null, m_plan)?.ShowSkillInBrowser(skill!);
        }

        #endregion


        #region Context menu

        /// <summary>
        /// Context menu opening, updates the "plan to" menus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextMenu_Opening(object? sender,CancelEventArgs e)
        {
            tsSeparatorBrowser.Visible = tvSkillList.SelectedNode != null;
            tsmCollapse.Enabled = tsmCollapse.Visible = m_allExpanded;
            tsmExpand.Enabled = tsmExpand.Visible = !tsmCollapse.Enabled;

            // Update "show in..." menu
            planToMenu.Visible =
                showInMenuSeparator.Visible =
                    showInSkillBrowserMenu.Visible =
                        showInSkillExplorerMenu.Visible = tvSkillList.SelectedNode != null;

            if (tvSkillList.SelectedNode == null)
                return;

            // "Plan to N" menus
            SkillLevel? skillLevel = tvSkillList.SelectedNode.Tag as SkillLevel;
            Skill? skill = skillLevel?.Skill;

            if (skill == null)
                return;

            planToMenu.Enabled = false;
            for (int i = 0; i <= 5; i++)
            {
                planToMenu.Enabled |= m_plan!.UpdatesRegularPlanToMenu(planToMenu.DropDownItems[i], skill, i);
            }
        }

        /// <summary>
        /// Context > Show in Skill Browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showInSkillBrowserMenu_Click(object? sender,EventArgs e)
        {
            Skill? skill = ((SkillLevel?)tvSkillList.SelectedNode?.Tag)?.Skill;

            // Open the skill browser
            PlanWindow.ShowPlanWindow(null, m_plan)?.ShowSkillInBrowser(skill!);
        }

        /// <summary>
        /// Context > Show in Skill Explorer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showInSkillExplorerMenu_Click(object? sender,EventArgs e)
        {
            Skill? skill = ((SkillLevel?)tvSkillList.SelectedNode?.Tag)?.Skill;

            // Open the skill explorer
            SkillExplorerWindow.ShowSkillExplorerWindow(skill?.Character, m_plan)?.ShowSkillInExplorer(skill!);
        }

        /// <summary>
        /// Treeview's context menu > Expand All
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmExpandAll_Click(object? sender,EventArgs e)
        {
            tvSkillList.ExpandAll();
            m_allExpanded = true;
        }

        /// <summary>
        /// Treeview's context menu > Collapse All
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsmCollapseAll_Click(object? sender,EventArgs e)
        {
            tvSkillList.CollapseAll();
            m_allExpanded = false;
        }

        /// <summary>
        /// Context > Plan To > Level N
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void planToMenuItem_Click(object? sender,EventArgs e)
        {
            ToolStripMenuItem menu = (ToolStripMenuItem)sender!;
            IPlanOperation? operation = menu.Tag as IPlanOperation;
            if (operation == null)
                return;

            PlanWindow? planWindow = ParentForm as PlanWindow;
            if (planWindow == null)
                return;

            PlanHelper.SelectPerform(new PlanToOperationWindow(operation), planWindow, operation);
        }

        #endregion
    }
}