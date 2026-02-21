// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common.Controls;
using EVEMon.Common.Controls.MultiPanel;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;

namespace EVEMon.SkillPlanner
{
    /// <summary>
    /// This window allows the user to deal with addition and suppresion of entries of plans.
    /// </summary>
    public partial class PlanToOperationWindow : EVEMonForm
    {
        private readonly IPlanOperation m_operation = null!;

        /// <summary>
        /// Designer constructor
        /// </summary>
        private PlanToOperationWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor for use in-code.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <exception cref="System.ArgumentException">@This window doesn't support empty operations.;operation</exception>
        /// <exception cref="System.ArgumentNullException">operation</exception>
        public PlanToOperationWindow(IPlanOperation operation)
            : this()
        {
            operation.ThrowIfNull(nameof(operation));

            if (operation.Type == PlanOperations.None)
                throw new ArgumentException(@"This window doesn't support empty operations.", nameof(operation));

            m_operation = operation;
            rootMultiPanel.SelectedPage = additionPage;
        }

        /// <summary>
        /// On load, selects the proper page.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Are we performing an addition ?
            if (m_operation.Type == PlanOperations.Addition)
            {
                Text = @"Add entries to plan";

                // Updates the other buttons
                previousButton.Visible = false;
                nextButton.DialogResult = DialogResult.OK;
                nextButton.Text = @"OK";
                nextButton.Focus();

                // Loads the listbox
                FillListBox(m_operation.AllEntriesToAdd, additionsListBox);

                // Priority controls
                if (m_operation.HighestPriorityForAddition < 1)
                {
                    priorityNumericBox.Minimum = 1;
                    priorityGroup.Visible = false;
                }
                else
                {
                    priorityNumericBox.Minimum = m_operation.HighestPriorityForAddition;
                    priorityLabel.Text = $"The highest priority you can set is {m_operation.HighestPriorityForAddition}";
                }
            }
                // Suppression 
            else
            {
                Text = @"Remove entries from plan";
                previousButton.Visible = m_operation.RemovablePrerequisites.Count() != 0;
                cancelButton.Focus();

                // Updates the selected page
                rootMultiPanel.SelectionChange += rootMultiPanel_SelectionChange;
                rootMultiPanel.SelectedPage = dependenciesSuppressionPage;

                // Loads the entries to remove listbox
                FillListBox(m_operation.AllEntriesToRemove, suppressionListBox);

                // Loads the useless prerequisites listbox
                FillListBox(m_operation.RemovablePrerequisites, uselessPrereqsListBox);
            }
        }

        /// <summary>
        /// Fetches the items to the given list box.
        /// </summary>
        private void FillListBox<T>(IEnumerable<T> items, ListBox listBox)
            where T : ISkillLevel
        {
            PlanScratchpad plan = new PlanScratchpad(m_operation.Plan.Character);
            plan.RebuildPlanFrom(items.Select(x => new PlanEntry(x.Skill, x.Level)));
            plan.FixPrerequisites();

            listBox.Items.Clear();
            foreach (PlanEntry entry in plan)
            {
                string name = entry.ToString();

                if (m_operation.Type == PlanOperations.Addition)
                {
                    // Skip if the entry is already in the plan
                    if (m_operation.Plan.IsPlanned(entry.Skill, entry.Level))
                        continue;
                }
                else
                {
                    // On creation of "entries to remove" listbox (first pass),
                    // skip if entry type is of prerequisite.
                    // "Useless prerequisites" listbox is created on second pass
                    // and in that case entry type is of type planned.
                    if (entry.Type == PlanEntryType.Prerequisite)
                        continue;

                    if (entry.Type == PlanEntryType.Planned)
                        name += " (planned)";
                }

                listBox.Items.Add(name);
            }
        }

        /// <summary>
        /// When the selected page changes, we update the buttons. Only occurs for suppression.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void rootMultiPanel_SelectionChange(object? sender,MultiPanelSelectionChangeEventArgs args)
        {
            // When there are useless prereqs, we offer the user to remove them on a second page.
            // When there aren't any, we just have one page.

            // Guess whether we're on the final page (2/2 or 1/1)
            bool isFinal;
            if (rootMultiPanel.SelectedPage == uselessPrereqsSuppressionPage)
                isFinal = true;
            else
                isFinal = !m_operation.RemovablePrerequisites.Any();

            // Final page ? 
            if (isFinal)
            {
                nextButton.Text = @"OK";
                nextButton.DialogResult = DialogResult.OK;
                previousButton.Enabled = true;
            }
            else
            {
                nextButton.Text = @"&Next >";
                nextButton.DialogResult = DialogResult.None;
                previousButton.Enabled = false;
            }
        }

        /// <summary>
        /// Click on the previous button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void previousButton_Click(object? sender,EventArgs e)
        {
            rootMultiPanel.SelectedPage = dependenciesSuppressionPage;
        }

        /// <summary>
        /// Click on the Next/OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nextButton_Click(object? sender,EventArgs e)
        {
            // Are we on the first page ? Then it's "Next >".
            if (nextButton.DialogResult == DialogResult.None)
            {
                rootMultiPanel.SelectedPage = uselessPrereqsSuppressionPage;
                return;
            }

            // Then we're on the last page and the button is now "OK"
            if (m_operation.Type == PlanOperations.Addition)
                m_operation.PerformAddition((int)priorityNumericBox.Value);
            else
                m_operation.PerformSuppression(uselessPrereqsCheckbox.Checked);

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Click on the cancel button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object? sender,EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}