// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Constants;
using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the Clones tab. Exposes active clone, jump clones,
    /// implant summaries, and clone jump timer for display.
    /// </summary>
    public sealed class ClonesViewModel : CharacterViewModelBase
    {
        public ClonesViewModel() { }

        public ClonesViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher) { }

        // --- Data properties ---

        public string HomeStationName { get; private set; } = "Unknown";
        public CloneDisplayEntry? ActiveClone { get; private set; }
        public IReadOnlyList<CloneDisplayEntry> JumpClones { get; private set; } = Array.Empty<CloneDisplayEntry>();
        public int JumpCloneCount { get; private set; }
        public bool CloneJumpAvailable { get; private set; }
        public string CloneJumpStatusText { get; private set; } = "";
        public string LastCloneJumpText { get; private set; } = "";
        public string HomeStationChangedText { get; private set; } = "";

        protected override void OnCharacterChanged()
        {
            Refresh();
        }

        public void Refresh()
        {
            var character = Character;
            if (character == null) return;

            // Home station — may not resolve when ServiceLocator isn't bootstrapped (tests)
            try
            {
                var station = character.HomeStation;
                HomeStationName = station?.Name ?? "Unknown";
            }
            catch
            {
                HomeStationName = "Unknown";
            }

            // Clone jump timer — Infomorph Synchronizing reduces cooldown by 1h per level
            // Base: 24h, Level V: 19h
            int syncLevel = character.LastConfirmedSkillLevel(DBConstants.InfomorphSynchronizingSkillID);
            int cooldownHours = 24 - syncLevel;
            var lastJump = character.JumpCloneLastJumpDate;
            if (lastJump > DateTime.MinValue)
            {
                var cooldownEnd = lastJump.AddHours(cooldownHours);
                var remaining = cooldownEnd - DateTime.UtcNow;
                CloneJumpAvailable = remaining <= TimeSpan.Zero;
                CloneJumpStatusText = CloneJumpAvailable
                    ? "Ready"
                    : TimeFormatHelper.FormatRemaining(remaining);
                LastCloneJumpText = lastJump.ToString("yyyy-MM-dd HH:mm");
            }
            else
            {
                CloneJumpAvailable = true;
                CloneJumpStatusText = "Ready";
                LastCloneJumpText = "Never";
            }

            // Home station change date
            var remoteDate = character.RemoteStationDate;
            HomeStationChangedText = remoteDate > DateTime.MinValue
                ? remoteDate.ToString("yyyy-MM-dd HH:mm")
                : "Never";

            // Active clone
            var implantSets = character.ImplantSets;
            ActiveClone = BuildCloneEntry(implantSets.ActiveClone, true);

            // Jump clones — ImplantSetCollection.Enumerate() yields: ActiveClone, then
            // m_cloneSets (jump clones), then m_customSets. Skip ActiveClone (first item).
            var jumpClones = new List<CloneDisplayEntry>();
            bool isFirst = true;
            foreach (var set in implantSets)
            {
                if (isFirst) { isFirst = false; continue; }
                jumpClones.Add(BuildCloneEntry(set, false));
            }
            jumpClones.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            JumpClones = jumpClones;
            JumpCloneCount = jumpClones.Count;

            OnPropertyChanged(nameof(HomeStationName));
            OnPropertyChanged(nameof(ActiveClone));
            OnPropertyChanged(nameof(JumpClones));
            OnPropertyChanged(nameof(JumpCloneCount));
            OnPropertyChanged(nameof(CloneJumpAvailable));
            OnPropertyChanged(nameof(CloneJumpStatusText));
            OnPropertyChanged(nameof(LastCloneJumpText));
            OnPropertyChanged(nameof(HomeStationChangedText));
        }

        private static CloneDisplayEntry BuildCloneEntry(ImplantSet set, bool isActive)
        {
            var implants = new List<ImplantDisplayEntry>();
            foreach (var implant in set)
            {
                if (implant.ID <= 0) continue; // empty slot
                string slotLabel = implant.Slot switch
                {
                    ImplantSlots.Intelligence => "Int",
                    ImplantSlots.Perception => "Per",
                    ImplantSlots.Willpower => "Wil",
                    ImplantSlots.Charisma => "Cha",
                    ImplantSlots.Memory => "Mem",
                    _ => $"Slot {(int)implant.Slot + 1}"
                };
                implants.Add(new ImplantDisplayEntry(implant.ID, implant.Name, slotLabel, implant.Bonus));
            }

            // Build compact summary: "+5 Int, +5 Per, ..." for attribute implants
            var attrImplants = implants.Where(i => i.Bonus > 0).ToList();
            string summary = attrImplants.Count > 0
                ? string.Join(", ", attrImplants.Select(i => $"+{i.Bonus} {i.SlotLabel}"))
                : "No attribute implants";

            // Non-attribute implants (slots 6-10)
            var hardwirings = implants.Where(i => i.Bonus == 0).ToList();

            return new CloneDisplayEntry(set.Name, isActive, summary, implants, hardwirings.Count);
        }
    }

    /// <summary>Display data for a single clone (active or jump).</summary>
    public sealed class CloneDisplayEntry
    {
        public CloneDisplayEntry(string name, bool isActive, string implantSummary,
            IReadOnlyList<ImplantDisplayEntry> implants, int hardwiringCount)
        {
            Name = name;
            IsActive = isActive;
            ImplantSummary = implantSummary;
            Implants = implants;
            HardwiringCount = hardwiringCount;
        }

        public string Name { get; }
        public bool IsActive { get; }
        public string ImplantSummary { get; }
        public IReadOnlyList<ImplantDisplayEntry> Implants { get; }
        public int HardwiringCount { get; }
        public int TotalImplantCount => Implants.Count;
    }

    /// <summary>Display data for a single implant in a clone.</summary>
    public sealed class ImplantDisplayEntry
    {
        public ImplantDisplayEntry(int typeId, string name, string slotLabel, long bonus)
        {
            TypeId = typeId;
            Name = name;
            SlotLabel = slotLabel;
            Bonus = bonus;
        }

        public int TypeId { get; }
        public string Name { get; }
        public string SlotLabel { get; }
        public long Bonus { get; }
    }
}
