// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using System.Windows.Forms;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Extensions;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;

namespace EVEMon.Common.Helpers
{
    /// <summary>
    /// Helper for the "Plan To" and "Remove" menus.
    /// </summary>
    public static class PlanHelper
    {
        /// <summary>
        /// Updates a regular "Plan to X" menu : text, tag, enable/disable.
        /// </summary>
        /// <param name="plan">The plan.</param>
        /// <param name="menu">The menu.</param>
        /// <param name="skill">The skill.</param>
        /// <param name="level">The level.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">menu
        /// or
        /// plan</exception>
        public static bool UpdatesRegularPlanToMenu(this Plan plan, ToolStripItem menu, Skill skill, int level)
        {
            menu.ThrowIfNull(nameof(menu));

            plan.ThrowIfNull(nameof(plan));

            menu.Text = level == 0 ? "Remove" : $"Level {level}";

            menu.Enabled = plan.EnablePlanTo(skill, level);
            if (menu.Enabled)
            {
                IPlanOperation operation = plan.TryPlanTo(skill, level);
                menu.Tag = operation;
                if (RequiresWindow(operation))
                    menu.Text += @"...";
            }

            ToolStripMenuItem menuItem = menu as ToolStripMenuItem;
            if (menuItem != null)
                menuItem.Checked = plan.GetPlannedLevel(skill) == level;

            return menu.Enabled;
        }

        /// <summary>
        /// Checks whether the given operation absolutely requires a confirmation from the user.
        /// True when there are dependencies to remove.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <returns></returns>
        public static bool RequiresWindow(IPlanOperation operation)
        {
            if (operation?.Type != PlanOperations.Suppression)
                return false;

            return operation.SkillsToRemove.Count() != operation.AllEntriesToRemove.Count();
        }

        /// <summary>
        /// Performs the action for the "Plan To N" and "Remove" menu options, in a silent way whenever possible.
        /// </summary>
        /// <param name="operationForm">The operation form.</param>
        /// <param name="parentForm">The parent form.</param>
        /// <param name="operation">The operation.</param>
        private static void PerformSilently(Form operationForm, IWin32Window parentForm, IPlanOperation operation)
        {
            if (operation == null)
                return;

            // A window is required
            if (RequiresWindow(operation) && Perform(operationForm, parentForm) != DialogResult.OK)
                return;

            // Silent way
            operation.Perform();
        }

        /// <summary>
        /// Performs the action for the "Plan To N" and "Remove" menu options.
        /// </summary>
        /// <param name="operationForm">The operation form.</param>
        /// <param name="parentForm">The parent form.</param>
        /// <returns></returns>
        public static DialogResult Perform(Form operationForm, IWin32Window parentForm)
        {
            using (Form window = operationForm)
            {
                window.ShowDialog(parentForm);
                return window.DialogResult;
            }
        }

        /// <summary>
        /// Selects which type of Perform will be called according to user settings.
        /// </summary>
        /// <param name="operationForm">The operation form.</param>
        /// <param name="parentForm">The parent form.</param>
        /// <param name="operation">The operation.</param>
        public static void SelectPerform(Form operationForm, IWin32Window parentForm, IPlanOperation operation)
        {
            if (operation == null)
                return;

            if (Settings.UI.PlanWindow.UseAdvanceEntryAddition && operation.Type == PlanOperations.Addition)
            {
                Perform(operationForm, parentForm);
                return;
            }

            PerformSilently(operationForm, parentForm, operation);
        }
    }
}