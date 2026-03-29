// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Models;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the Character Skill Comparison window.
    /// Characters as columns, skills as rows, grouped by skill group.
    /// Supports filtering by text and showing only differences.
    /// </summary>
    public sealed class CharacterComparisonViewModel : IDisposable
    {
        private readonly List<Character> _selectedCharacters = new();
        private string _textFilter = string.Empty;
        private bool _showDifferencesOnly;
        private List<ComparisonGroupEntry> _groups = new();
        private List<ComparisonGroupEntry> _visibleGroups = new();

        /// <summary>Characters being compared (max 6).</summary>
        public IReadOnlyList<Character> SelectedCharacters => _selectedCharacters;

        /// <summary>All skill groups with comparison data.</summary>
        public IReadOnlyList<ComparisonGroupEntry> Groups => _visibleGroups;

        /// <summary>Total skills shown after filtering.</summary>
        public int VisibleSkillCount => _visibleGroups.Sum(g => g.VisibleSkills.Count);

        /// <summary>Total skills where at least one character differs.</summary>
        public int DifferenceCount => _groups.Sum(g => g.AllSkills.Count(s => s.IsDifferent));

        public string TextFilter
        {
            get => _textFilter;
            set
            {
                _textFilter = value ?? string.Empty;
                ApplyFilter();
            }
        }

        public bool ShowDifferencesOnly
        {
            get => _showDifferencesOnly;
            set
            {
                _showDifferencesOnly = value;
                ApplyFilter();
            }
        }

        public bool AddCharacter(Character character)
        {
            if (_selectedCharacters.Count >= 10) return false;
            if (_selectedCharacters.Any(c => c.CharacterID == character.CharacterID)) return false;
            _selectedCharacters.Add(character);
            Rebuild();
            return true;
        }

        public void RemoveCharacter(Character character)
        {
            _selectedCharacters.RemoveAll(c => c.CharacterID == character.CharacterID);
            Rebuild();
        }

        public void Rebuild()
        {
            if (_selectedCharacters.Count == 0)
            {
                _groups = new List<ComparisonGroupEntry>();
                _visibleGroups = _groups;
                return;
            }

            // Use the first character's skill groups as the template
            // (all characters share the same static skill tree)
            var reference = _selectedCharacters[0];
            var groups = new List<ComparisonGroupEntry>();

            foreach (var skillGroup in reference.SkillGroups.OrderBy(g => g.Name))
            {
                var skills = new List<ComparisonSkillEntry>();

                foreach (var skill in skillGroup.Where(s => s.IsPublic || s.IsKnown).OrderBy(s => s.Name))
                {
                    var levels = new int[_selectedCharacters.Count];
                    var isKnown = new bool[_selectedCharacters.Count];

                    for (int i = 0; i < _selectedCharacters.Count; i++)
                    {
                        var charSkill = _selectedCharacters[i].Skills[skill.ID];
                        if (charSkill != null && charSkill.IsKnown)
                        {
                            levels[i] = (int)charSkill.LastConfirmedLvl;
                            isKnown[i] = true;
                        }
                    }

                    bool isDifferent = false;
                    if (_selectedCharacters.Count > 1)
                    {
                        int firstLevel = levels[0];
                        bool firstKnown = isKnown[0];
                        for (int i = 1; i < levels.Length; i++)
                        {
                            if (levels[i] != firstLevel || isKnown[i] != firstKnown)
                            {
                                isDifferent = true;
                                break;
                            }
                        }
                    }

                    skills.Add(new ComparisonSkillEntry(
                        skill.Name, skill.ID, levels, isKnown, isDifferent));
                }

                if (skills.Count > 0)
                    groups.Add(new ComparisonGroupEntry(skillGroup.Name, skills));
            }

            _groups = groups;
            ApplyFilter();
        }

        public void CollapseAll()
        {
            foreach (var g in _groups) g.IsExpanded = false;
        }

        public void ExpandAll()
        {
            foreach (var g in _groups) g.IsExpanded = true;
        }

        private void ApplyFilter()
        {
            var result = new List<ComparisonGroupEntry>();

            foreach (var group in _groups)
            {
                var filtered = group.AllSkills.AsEnumerable();

                if (_showDifferencesOnly)
                    filtered = filtered.Where(s => s.IsDifferent);

                if (!string.IsNullOrEmpty(_textFilter))
                    filtered = filtered.Where(s =>
                        s.SkillName.Contains(_textFilter, StringComparison.OrdinalIgnoreCase));

                var list = filtered.ToList();
                group.VisibleSkills = list;

                if (list.Count > 0)
                    result.Add(group);
            }

            _visibleGroups = result;
        }

        public void Dispose() { }
    }

    /// <summary>A skill group in the comparison grid.</summary>
    public sealed class ComparisonGroupEntry
    {
        public string GroupName { get; }
        public List<ComparisonSkillEntry> AllSkills { get; }
        public List<ComparisonSkillEntry> VisibleSkills { get; set; }
        public bool IsExpanded { get; set; }
        public string CountText => $"{VisibleSkills.Count} skills";

        public ComparisonGroupEntry(string groupName, List<ComparisonSkillEntry> skills)
        {
            GroupName = groupName;
            AllSkills = skills;
            VisibleSkills = skills;
            IsExpanded = false;
        }
    }

    /// <summary>A single skill row in the comparison grid.</summary>
    public sealed class ComparisonSkillEntry
    {
        public string SkillName { get; }
        public int SkillID { get; }
        /// <summary>Skill level per character (index matches SelectedCharacters).</summary>
        public int[] Levels { get; }
        /// <summary>Whether the character has injected/trained this skill.</summary>
        public bool[] IsKnown { get; }
        /// <summary>True if any character's level differs from another.</summary>
        public bool IsDifferent { get; }

        public ComparisonSkillEntry(string skillName, int skillId,
            int[] levels, bool[] isKnown, bool isDifferent)
        {
            SkillName = skillName;
            SkillID = skillId;
            Levels = levels;
            IsKnown = isKnown;
            IsDifferent = isDifferent;
        }
    }
}
