// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.ObjectModel;
using System.Text;
using EveLens.Common.Collections;
using EveLens.Common.Constants;
using EveLens.Common.Extensions;
using EveLens.Common.Helpers;
using EveLens.Common.Interfaces;

namespace EveLens.Common.Models
{
    /// <summary>
    /// Represents the result of a remapping.
    /// </summary>
    public sealed class RemappingResult
    {
        /// <summary>
        /// Constructor without any remapping point associated.
        /// </summary>
        /// <param name="baseScratchpad">The base scratchpad.</param>
        /// <exception cref="System.ArgumentNullException">baseScratchpad</exception>
        public RemappingResult(CharacterScratchpad baseScratchpad)
        {
            baseScratchpad.ThrowIfNull(nameof(baseScratchpad));

            Skills = new Collection<ISkillLevel>();
            BaseScratchpad = baseScratchpad;
            StartTime = BaseScratchpad.TrainingTime;
        }

        /// <summary>
        /// Constructor for a result bound to a remapping point.
        /// </summary>
        /// <param name="point">Associated remapping point, may be null.</param>
        /// <param name="baseScratchpad"></param>
        public RemappingResult(RemappingPoint point, CharacterScratchpad baseScratchpad)
            : this(baseScratchpad)
        {
            if (point == null)
                return;

            Point = point;
        }

        /// <summary>
        /// Constructor for a manually edited result from a base result.
        /// </summary>
        /// <param name="result">Associated remapping point, may be null.</param>
        /// <param name="bestScratchpad">The best scratchpad.</param>
        /// <exception cref="System.ArgumentNullException">result</exception>
        public RemappingResult(RemappingResult result, CharacterScratchpad bestScratchpad)
            : this(result?.Point, result?.BaseScratchpad)
        {
            result.ThrowIfNull(nameof(result));

            Skills.AddRange(result.Skills);
            BestScratchpad = bestScratchpad;
        }

        /// <summary>
        /// Gets the optimized plan.
        /// </summary>
        public Collection<ISkillLevel> Skills { get; }

        /// <summary>
        /// Gets the remapping point associated with that remapping.
        /// May be null if a remapping was automatically added at the beginning of the training.
        /// </summary>
        public RemappingPoint Point { get; }

        /// <summary>
        /// Gets the best scratchpad after the remapping.
        /// </summary>
        public CharacterScratchpad BaseScratchpad { get; }

        /// <summary>
        /// Gets the best scratchpad after the remapping.
        /// </summary>
        public CharacterScratchpad BestScratchpad { get; private set; }

        /// <summary>
        /// Gets the training duration with the best remapping.
        /// </summary>
        public TimeSpan BestDuration { get; private set; }

        /// <summary>
        /// Gets the base training duration before the remapping.
        /// </summary>
        public TimeSpan BaseDuration { get; private set; }

        /// <summary>
        /// Gets the time when this remapping was done.
        /// </summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        /// Computes an optimized scratchpad, then call <see cref="Update"/>.
        /// </summary>
        /// <param name="maxDuration">The max duration to take into account for optimization.</param>
        /// <returns></returns>
        public void Optimize(TimeSpan maxDuration)
        {
            BestScratchpad = AttributesOptimizer.Optimize(Skills, BaseScratchpad, maxDuration);
            Update();
        }

        /// <summary>
        /// Updates the times and, when any, the associated remapping point.
        /// </summary>
        /// <returns></returns>
        public void Update()
        {
            // Optimize
            BaseDuration = BaseScratchpad.After(Skills).TrainingTime.Subtract(StartTime);
            BestDuration = BestScratchpad.After(Skills).TrainingTime.Subtract(StartTime);

            // Update the underlying remapping point
            Point?.SetBaseAttributes(BestScratchpad, BaseScratchpad);
        }

        /// <summary>
        /// Gets a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder().
                Append("i").Append(BestScratchpad.Intelligence.Base.ToString(CultureConstants.DefaultCulture)).
                Append(" p").Append(BestScratchpad.Perception.Base.ToString(CultureConstants.DefaultCulture)).
                Append(" c").Append(BestScratchpad.Charisma.Base.ToString(CultureConstants.DefaultCulture)).
                Append(" w").Append(BestScratchpad.Willpower.Base.ToString(CultureConstants.DefaultCulture)).
                Append(" m").Append(BestScratchpad.Memory.Base.ToString(CultureConstants.DefaultCulture));

            return builder.ToString();
        }
    }
}