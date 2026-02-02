using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EVEMon.Common.Constants;
using EVEMon.Common.Extensions;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;

namespace EVEMon.Common.Helpers
{
    public static class AttributesOptimizer
    {
        #region Helper Methods

        /// <summary>
        /// Applies the simulated booster bonus from the plan to a scratchpad.
        /// </summary>
        /// <param name="scratchpad">The scratchpad to modify.</param>
        /// <param name="plan">The plan containing booster settings.</param>
        private static void ApplyPlanBoosterBonus(CharacterScratchpad scratchpad, BasePlan plan)
        {
            if (!plan.HasBoosterSimulation)
                return;

            int boosterBonus = plan.SimulatedBoosterBonus;
            scratchpad.Memory.BoosterBonus = boosterBonus;
            scratchpad.Charisma.BoosterBonus = boosterBonus;
            scratchpad.Intelligence.BoosterBonus = boosterBonus;
            scratchpad.Perception.BoosterBonus = boosterBonus;
            scratchpad.Willpower.BoosterBonus = boosterBonus;
        }

        #endregion


        #region Computations

        /// <summary>
        /// Compute the best possible attributes to fulfill the given trainings array
        /// </summary>
        /// <param name="skills"></param>
        /// <param name="baseScratchpad"></param>
        /// <param name="maxDuration"></param>
        /// <returns></returns>
        internal static CharacterScratchpad Optimize<T>(IEnumerable<T> skills, CharacterScratchpad baseScratchpad,
                                                       TimeSpan maxDuration)
            where T : ISkillLevel
        {
            CharacterScratchpad bestScratchpad = new CharacterScratchpad(baseScratchpad);
            CharacterScratchpad tempScratchpad = new CharacterScratchpad(baseScratchpad);
            TimeSpan baseTime = baseScratchpad.TrainingTime;
            TimeSpan bestTime = TimeSpan.MaxValue;
            int bestSkillCount = 0;

            // Now, we have the points to spend, let's perform all the
            // combinations (less than 11^4 = 14,641)
            for (int per = 0; per <= EveConstants.MaxRemappablePointsPerAttribute; per++)
            {
                // WIL
                int maxWillpower = EveConstants.SpareAttributePointsOnRemap - per;
                for (int will = 0; will <= maxWillpower && will <= EveConstants.MaxRemappablePointsPerAttribute; will++)
                {
                    // INT
                    int maxIntelligence = maxWillpower - will;
                    for (int intell = 0;
                         intell <= maxIntelligence && intell <= EveConstants.MaxRemappablePointsPerAttribute;
                         intell++)
                    {
                        // MEM
                        int maxMemory = maxIntelligence - intell;
                        for (int mem = 0; mem <= maxMemory && mem <= EveConstants.MaxRemappablePointsPerAttribute; mem++)
                        {
                            // CHA
                            int cha = maxMemory - mem;

                            // Reject invalid combinations
                            if (cha > EveConstants.MaxRemappablePointsPerAttribute)
                                continue;

                            // Resets the scratchpad
                            tempScratchpad.Reset();

                            // Set new attributes
                            tempScratchpad.Memory.Base = mem + EveConstants.CharacterBaseAttributePoints;
                            tempScratchpad.Charisma.Base = cha + EveConstants.CharacterBaseAttributePoints;
                            tempScratchpad.Willpower.Base = will + EveConstants.CharacterBaseAttributePoints;
                            tempScratchpad.Perception.Base = per + EveConstants.CharacterBaseAttributePoints;
                            tempScratchpad.Intelligence.Base = intell + EveConstants.CharacterBaseAttributePoints;

                            // Train skills
                            int tempSkillCount = 0;
                            foreach (T skill in skills)
                            {
                                tempSkillCount++;
                                tempScratchpad.Train(skill);

                                // Did it go over max duration ?
                                if (tempScratchpad.TrainingTime - baseTime > maxDuration)
                                    break;

                                // Did it go over the best time so far without training more skills ?
                                if (tempSkillCount <= bestSkillCount && tempScratchpad.TrainingTime > bestTime)
                                    break;
                            }

                            // Did it manage to train more skills before the max duration, 
                            // or did it train the same number of skills in a lesser time ?
                            if (tempSkillCount <= bestSkillCount &&
                                (tempSkillCount != bestSkillCount || tempScratchpad.TrainingTime >= bestTime))
                                continue;

                            bestScratchpad.Reset();
                            bestScratchpad.Memory.Base = tempScratchpad.Memory.Base;
                            bestScratchpad.Charisma.Base = tempScratchpad.Charisma.Base;
                            bestScratchpad.Willpower.Base = tempScratchpad.Willpower.Base;
                            bestScratchpad.Perception.Base = tempScratchpad.Perception.Base;
                            bestScratchpad.Intelligence.Base = tempScratchpad.Intelligence.Base;
                            bestTime = tempScratchpad.TrainingTime;
                            bestSkillCount = tempSkillCount;
                        }
                    }
                }
            }

            // Return the best scratchpad found
            return bestScratchpad;
        }

        /// <summary>
        /// Generate a trainings array from a plan
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        public static ICollection<RemappingResult> OptimizeFromPlanAndRemappingPoints(BasePlan plan)
        {
            Collection<RemappingResult> results = GetResultsFromRemappingPoints(plan);
            foreach (RemappingResult result in results)
            {
                result.Optimize(TimeSpan.MaxValue);
            }
            return results;
        }

        /// <summary>
        /// Gets the list of remapping results from a plan.
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">plan</exception>
        public static Collection<RemappingResult> GetResultsFromRemappingPoints(BasePlan plan)
        {
            plan.ThrowIfNull(nameof(plan));

            CharacterScratchpad scratchpad = new CharacterScratchpad(plan.Character.After(plan.ChosenImplantSet));
            ApplyPlanBoosterBonus(scratchpad, plan);
            Collection<RemappingResult> remappingList = new Collection<RemappingResult>();
            Collection<ISkillLevel> list = new Collection<ISkillLevel>();

            // Scroll through the entries and split it into remappings
            foreach (PlanEntry entry in plan)
            {
                // Ends the current remapping and start a new one
                if (entry.Remapping != null)
                {
                    // Creates a new remapping
                    RemappingResult remapping = new RemappingResult(entry.Remapping, scratchpad.Clone());
                    remappingList.Add(remapping);
                    list = remapping.Skills;
                }

                // Add this skill to the training list
                scratchpad.Train(entry);
                list.Add(entry);
            }

            // Return
            return remappingList;
        }

        /// <summary>
        /// Compute the best remapping for the first year of this plan.
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">plan</exception>
        public static RemappingResult OptimizeFromFirstYearOfPlan(BasePlan plan)
        {
            plan.ThrowIfNull(nameof(plan));

            CharacterScratchpad scratchpad = new CharacterScratchpad(plan.Character.After(plan.ChosenImplantSet));
            ApplyPlanBoosterBonus(scratchpad, plan);
            RemappingResult remapping = new RemappingResult(scratchpad);

            // Scroll through the entries and split it into remappings
            foreach (PlanEntry entry in plan)
            {
                remapping.Skills.Add(entry);
            }

            // Compute
            remapping.Optimize(TimeSpan.FromDays(365.0));
            return remapping;
        }

        /// <summary>
        /// Generate a trainings array from the skills already know by a character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="plan">The plan.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">character or plan</exception>
        public static RemappingResult OptimizeFromCharacter(Character character, BasePlan plan)
        {
            character.ThrowIfNull(nameof(character));

            plan.ThrowIfNull(nameof(plan));

            // Create a character without any skill
            CharacterScratchpad scratchpad = new CharacterScratchpad(character.After(plan.ChosenImplantSet));
            ApplyPlanBoosterBonus(scratchpad, plan);
            scratchpad.ClearSkills();

            // Create a new plan
            Plan newPlan = new Plan(scratchpad);

            // Add all trained skill levels that the character has trained so far
            foreach (Skill skill in character.Skills)
            {
                newPlan.PlanTo(skill, skill.Level);
            }

            // Create a new remapping
            RemappingResult remapping = new RemappingResult(scratchpad);

            // Add those skills to the remapping
            foreach (PlanEntry entry in newPlan)
            {
                remapping.Skills.Add(entry);
            }

            // Optimize
            remapping.Optimize(TimeSpan.MaxValue);
            return remapping;
        }

        #endregion
    }
}