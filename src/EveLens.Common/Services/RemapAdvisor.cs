// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;

namespace EveLens.Common.Services
{
    /// <summary>What a character should do about a neural remap for one plan.</summary>
    public enum RemapAdviceState
    {
        /// <summary>The character's attributes are not known well enough to advise.</summary>
        NoData,

        /// <summary>The plan has nothing left to train.</summary>
        EmptyPlan,

        /// <summary>The plan already carries remap points.</summary>
        AlreadyPlanned,

        /// <summary>No remap saves enough time to be worth spending.</summary>
        NoRemapNeeded,

        /// <summary>A remap makes the plan faster and the character can remap now.</summary>
        RemapNow,

        /// <summary>A remap makes the plan faster but the character has none to spend yet.</summary>
        RemapLater,
    }

    /// <summary>The answer for one plan: the state, and the numbers that go with it.</summary>
    public sealed class RemapAdvice
    {
        /// <summary>The state the plan and character are in.</summary>
        public RemapAdviceState State { get; init; }

        /// <summary>
        /// The attributes to remap to; empty unless <see cref="State"/> is
        /// <see cref="RemapAdviceState.RemapNow"/> or <see cref="RemapAdviceState.RemapLater"/>.
        /// </summary>
        public IReadOnlyDictionary<EveAttribute, long> Attributes { get; init; }
            = new Dictionary<EveAttribute, long>();

        /// <summary>
        /// The skill the plan trains last before the remap; empty when the remap comes first.
        /// </summary>
        public string AfterSkill { get; init; } = string.Empty;

        /// <summary>The plan's duration as it stands.</summary>
        public TimeSpan CurrentDuration { get; init; }

        /// <summary>The plan's duration with the advised remap.</summary>
        public TimeSpan OptimizedDuration { get; init; }

        /// <summary>The time the advised remap saves.</summary>
        public TimeSpan TimeSaved => CurrentDuration - OptimizedDuration;

        /// <summary>
        /// The temporary bonus on every attribute, counted for the current attributes and
        /// for the advised ones alike.
        /// </summary>
        public long TemporaryBonus { get; init; }

        /// <summary>The remaps the character can spend.</summary>
        public RemapAvailability Availability { get; init; } = RemapAvailability.TimedOnly;

        /// <summary>The number of remap points already on the plan.</summary>
        public int PlannedRemapCount { get; init; }

        /// <summary>
        /// The schedule behind the advice, for <see cref="RemapPlanningService.Apply"/>;
        /// null unless a remap is advised.
        /// </summary>
        public RemapPlanningService.RemapProposal? Proposal { get; init; }
    }

    /// <summary>
    /// Turns a plan and its character into a single piece of remap advice.
    /// </summary>
    public static class RemapAdvisor
    {
        /// <summary>The saving a remap has to beat before it is advised.</summary>
        public static readonly TimeSpan WorthwhileSaving = TimeSpan.FromHours(12);

        /// <summary>True when the saving is large enough to advise spending a remap on.</summary>
        public static bool IsWorthwhile(TimeSpan timeSaved) => timeSaved > WorthwhileSaving;

        /// <summary>
        /// Works out the advice for the plan with at most one remap. The plan is not modified.
        /// </summary>
        /// <param name="plan">The plan, in the order it trains.</param>
        /// <param name="utcNow">The time the character's remaps are judged at.</param>
        /// <param name="cloneOverride">Analyse as Alpha or Omega; null follows the character.</param>
        /// <exception cref="ArgumentNullException">plan</exception>
        public static RemapAdvice Advise(BasePlan plan, DateTime utcNow,
            AccountStatusMode? cloneOverride = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            if (plan.Character is not Character character || !character.AttributesAreConsistent)
                return new RemapAdvice { State = RemapAdviceState.NoData };

            RemapAvailability availability = character.RemapAvailability;
            long temporaryBonus = character.Intelligence.TemporaryBonus;

            if (!plan.Any(entry => entry.Skill != null))
            {
                return new RemapAdvice
                {
                    State = RemapAdviceState.EmptyPlan,
                    Availability = availability,
                    TemporaryBonus = temporaryBonus,
                };
            }

            int planned = plan.Count(entry => entry.Remapping != null);
            RemapPlanningService.RemapProposal proposal =
                RemapPlanningService.ProposeAtAttributeBoundaries(plan, maxRemaps: 1,
                    cloneOverride: cloneOverride);

            if (planned > 0)
            {
                return new RemapAdvice
                {
                    State = RemapAdviceState.AlreadyPlanned,
                    PlannedRemapCount = planned,
                    CurrentDuration = proposal.CurrentDuration,
                    OptimizedDuration = proposal.CurrentDuration,
                    Availability = availability,
                    TemporaryBonus = temporaryBonus,
                };
            }

            if (proposal.Remaps.Count == 0 || !IsWorthwhile(proposal.TimeSaved))
            {
                return new RemapAdvice
                {
                    State = RemapAdviceState.NoRemapNeeded,
                    CurrentDuration = proposal.CurrentDuration,
                    OptimizedDuration = proposal.CurrentDuration,
                    Availability = availability,
                    TemporaryBonus = temporaryBonus,
                };
            }

            RemapPlanningService.ProposedRemap remap = proposal.Remaps[0];
            return new RemapAdvice
            {
                State = availability.CanRemapAt(utcNow)
                    ? RemapAdviceState.RemapNow
                    : RemapAdviceState.RemapLater,
                Attributes = new Dictionary<EveAttribute, long>(remap.Attributes),
                AfterSkill = remap.StartsAfter,
                CurrentDuration = proposal.CurrentDuration,
                OptimizedDuration = proposal.OptimizedDuration,
                Availability = availability,
                TemporaryBonus = temporaryBonus,
                Proposal = proposal,
            };
        }
    }
}
