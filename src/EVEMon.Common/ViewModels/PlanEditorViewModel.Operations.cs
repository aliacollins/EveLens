using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Data;
using EVEMon.Common.Helpers;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Common.ViewModels
{
    // Operations partial: two-phase mutations, direct mutations, export.
    public sealed partial class PlanEditorViewModel
    {
        #region Two-Phase Operations (Prepare + Perform)

        /// <summary>
        /// Prepares a removal operation for the given entries.
        /// Returns null if there are no entries to remove.
        /// </summary>
        public IPlanOperation? PrepareRemoval(IEnumerable<PlanEntry> entries)
        {
            if (_plan == null || entries == null)
                return null;

            return _plan.TryRemoveSet(entries);
        }

        /// <summary>
        /// Prepares an addition operation for the given skill levels.
        /// </summary>
        public IPlanOperation? PrepareAddition(IEnumerable<ISkillLevel> skills, string note)
        {
            if (_plan == null || skills == null)
                return null;

            return _plan.TryAddSet(skills, note ?? string.Empty);
        }

        /// <summary>
        /// Prepares an operation to plan a skill to a given level.
        /// Returns null if the skill is already planned to that level.
        /// </summary>
        public IPlanOperation? PreparePlanTo(Skill skill, long level)
        {
            if (_plan == null || skill == null)
                return null;

            return _plan.TryPlanTo(skill, level);
        }

        /// <summary>
        /// Performs an operation using default behavior (default priority for additions,
        /// no prerequisite removal for suppressions).
        /// </summary>
        public void PerformOperation(IPlanOperation operation)
        {
            operation?.Perform();
        }

        /// <summary>
        /// Performs an addition operation with the specified priority.
        /// </summary>
        public void PerformAddition(IPlanOperation operation, int priority)
        {
            operation?.PerformAddition(priority);
        }

        /// <summary>
        /// Performs a suppression operation, optionally removing unused prerequisites.
        /// </summary>
        public void PerformSuppression(IPlanOperation operation, bool removePrereqs)
        {
            operation?.PerformSuppression(removePrereqs);
        }

        #endregion


        #region Direct Mutations

        /// <summary>
        /// Plans a skill to the given level (simple add, no two-phase).
        /// </summary>
        public void PlanTo(StaticSkill skill, long level)
        {
            _plan?.PlanTo(skill, level);
        }

        /// <summary>
        /// Tries to set the priority of the given entries. Returns false if a conflict arises.
        /// </summary>
        public bool TrySetPriority(IEnumerable<PlanEntry> entries, int priority)
        {
            if (_plan == null || _displayPlan == null || entries == null)
                return false;

            return _plan.TrySetPriority(_displayPlan, entries, priority);
        }

        /// <summary>
        /// Sets the priority of the given entries, fixing conflicts automatically.
        /// </summary>
        public void SetPriority(IEnumerable<PlanEntry> entries, int priority)
        {
            if (_plan == null || _displayPlan == null || entries == null)
                return;

            _plan.SetPriority(_displayPlan, entries, priority);
        }

        /// <summary>
        /// Changes the notes on the given entries and rebuilds the plan.
        /// </summary>
        public void ChangeNotes(IEnumerable<PlanEntry> entries, string notes)
        {
            if (_plan == null || _displayPlan == null || entries == null)
                return;

            foreach (PlanEntry entry in entries)
            {
                entry.Notes = notes;
            }

            _plan.RebuildPlanFrom(_displayPlan, true);
        }

        /// <summary>
        /// Toggles the owned state for the skills of the given entries.
        /// If any are unowned, marks all as owned. Otherwise marks all as unowned.
        /// </summary>
        public void ToggleOwned(IEnumerable<PlanEntry> entries)
        {
            if (_plan == null || entries == null)
                return;

            var entryList = entries.ToList();
            bool markOwned = entryList.Any(x => !x.CharacterSkill.IsOwned);

            using (_plan.SuspendingEvents())
            {
                foreach (PlanEntry entry in entryList)
                {
                    entry.CharacterSkill.IsOwned = markOwned;
                }
            }
        }

        #endregion


        #region Export and Plan Creation

        /// <summary>
        /// Exports the given entries as text using the specified export settings.
        /// </summary>
        public string ExportSelectedAsText(IEnumerable<PlanEntry> entries, PlanExportSettings settings)
        {
            if (_plan == null || entries == null || settings == null)
                return string.Empty;

            Character? character = Character as Character;
            if (character == null)
                return string.Empty;

            Plan exportPlan = new Plan(character);
            IPlanOperation operation = exportPlan.TryAddSet(entries, "Exported from " + _plan.Name);
            operation.Perform();

            return PlanIOHelper.ExportAsText(exportPlan, settings);
        }

        /// <summary>
        /// Creates a new plan from the given entries.
        /// </summary>
        public Plan? CreatePlanFromSelection(IEnumerable<PlanEntry> entries, string name, string description)
        {
            if (_plan == null || entries == null)
                return null;

            Character? character = Character as Character;
            if (character == null)
                return null;

            Plan newPlan = new Plan(character) { Name = name, Description = description ?? string.Empty };
            IPlanOperation operation = newPlan.TryAddSet(entries, "Exported from " + _plan.Name);
            operation.Perform();

            return newPlan;
        }

        #endregion
    }
}
