// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.ViewModels
{
    public sealed class GlobalPlanDashboardViewModel
    {
        private List<GlobalPlanTemplate> _templates = new();
        private GlobalPlanTemplate? _selectedTemplate;
        private List<Character> _subscribedCharacters = new();
        private List<SkillComparisonRow> _comparisonRows = new();

        public IReadOnlyList<GlobalPlanTemplate> Templates => _templates;
        public GlobalPlanTemplate? SelectedTemplate => _selectedTemplate;
        public IReadOnlyList<Character> SubscribedCharacters => _subscribedCharacters;
        public IReadOnlyList<SkillComparisonRow> ComparisonRows => _comparisonRows;
        public TimeSpan LongestTotalTime { get; private set; }
        public TimeSpan ShortestTotalTime { get; private set; }
        public int TotalSkillsInTemplate => _selectedTemplate?.Entries.Count ?? 0;

        public void Refresh()
        {
            _templates = new List<GlobalPlanTemplate>(Settings.GlobalPlanTemplates);
        }

        public void SelectTemplate(GlobalPlanTemplate? template)
        {
            _selectedTemplate = template;
            if (template == null)
            {
                _subscribedCharacters = new List<Character>();
                _comparisonRows = new List<SkillComparisonRow>();
                return;
            }

            ResolveSubscribedCharacters();
            BuildComparisonRows();
        }

        private void ResolveSubscribedCharacters()
        {
            if (_selectedTemplate == null) return;

            var allChars = AppServices.Characters?.Cast<Character>().ToList() ?? new List<Character>();
            _subscribedCharacters = _selectedTemplate.SubscribedCharacterGuids
                .Select(guid => allChars.FirstOrDefault(c => c.Guid == guid))
                .Where(c => c != null)
                .Cast<Character>()
                .ToList();
        }

        private void BuildComparisonRows()
        {
            _comparisonRows = new List<SkillComparisonRow>();
            if (_selectedTemplate == null || _subscribedCharacters.Count == 0) return;

            foreach (var entry in _selectedTemplate.Entries)
            {
                var skill = StaticSkills.GetSkillByID(entry.SkillID);
                if (skill == null) continue;

                var row = new SkillComparisonRow
                {
                    SkillName = skill.Name,
                    SkillGroup = skill.Group?.Name ?? "Unknown",
                    TargetLevel = entry.Level,
                    Rank = (int)skill.Rank,
                    PrimaryAttribute = skill.PrimaryAttribute.ToString(),
                    SecondaryAttribute = skill.SecondaryAttribute.ToString(),
                    CharacterEntries = new List<CharacterSkillEntry>()
                };

                foreach (var character in _subscribedCharacters)
                {
                    long currentLevel = character.GetSkillLevel(skill);
                    var charEntry = new CharacterSkillEntry
                    {
                        CharacterName = character.Name,
                        CurrentLevel = (int)currentLevel,
                        TargetLevel = entry.Level,
                    };

                    if (currentLevel >= entry.Level)
                    {
                        charEntry.Status = SkillTrainingStatus.AlreadyTrained;
                        charEntry.TrainingTime = TimeSpan.Zero;
                    }
                    else
                    {
                        charEntry.TrainingTime = character.GetTrainingTime(skill, entry.Level);
                        charEntry.SpPerHour = (int)Math.Round(character.GetBaseSPPerHour(skill));
                        charEntry.Status = SkillTrainingStatus.NeedsTraining;
                    }

                    row.CharacterEntries.Add(charEntry);
                }

                _comparisonRows.Add(row);
            }

            ComputeSummary();
        }

        private void ComputeSummary()
        {
            if (_subscribedCharacters.Count == 0 || _comparisonRows.Count == 0)
            {
                LongestTotalTime = TimeSpan.Zero;
                ShortestTotalTime = TimeSpan.Zero;
                return;
            }

            var totals = new List<TimeSpan>();
            for (int i = 0; i < _subscribedCharacters.Count; i++)
            {
                int charIndex = i;
                var total = TimeSpan.Zero;
                foreach (var row in _comparisonRows)
                {
                    if (charIndex < row.CharacterEntries.Count)
                        total += row.CharacterEntries[charIndex].TrainingTime;
                }
                totals.Add(total);
            }

            LongestTotalTime = totals.Max();
            ShortestTotalTime = totals.Min();
        }

        #region Template CRUD

        public GlobalPlanTemplate CreateTemplate(string name)
        {
            var template = new GlobalPlanTemplate { Name = name };
            _templates.Add(template);
            Settings.GlobalPlanTemplates.Add(template);
            Settings.Save();
            return template;
        }

        public void DeleteTemplate(GlobalPlanTemplate template)
        {
            _templates.Remove(template);
            Settings.GlobalPlanTemplates.Remove(template);
            if (_selectedTemplate == template)
                SelectTemplate(null);
            Settings.Save();
        }

        public void RenameTemplate(GlobalPlanTemplate template, string newName)
        {
            template.Name = newName;
            Settings.Save();
        }

        #endregion

        #region Skill Management

        public bool AddSkill(int skillId, int level)
        {
            if (_selectedTemplate == null) return false;

            var skill = StaticSkills.GetSkillByID(skillId);
            if (skill == null) return false;

            var existing = _selectedTemplate.Entries.FirstOrDefault(
                e => e.SkillID == skillId && e.Level == level);
            if (existing != null) return false;

            _selectedTemplate.Entries.Add(new GlobalPlanTemplateEntry
            {
                SkillID = skillId,
                SkillName = skill.Name,
                Level = level
            });

            EnsurePrerequisites(skillId, level);
            Settings.Save();
            BuildComparisonRows();
            return true;
        }

        private void EnsurePrerequisites(int skillId, int targetLevel)
        {
            if (_selectedTemplate == null) return;

            var skill = StaticSkills.GetSkillByID(skillId);
            if (skill == null) return;

            for (int lvl = 1; lvl < targetLevel; lvl++)
            {
                if (!_selectedTemplate.Entries.Any(e => e.SkillID == skillId && e.Level == lvl))
                {
                    _selectedTemplate.Entries.Add(new GlobalPlanTemplateEntry
                    {
                        SkillID = skillId,
                        SkillName = skill.Name,
                        Level = lvl
                    });
                }
            }

            foreach (var prereq in skill.Prerequisites)
            {
                for (int lvl = 1; lvl <= (int)prereq.Level; lvl++)
                {
                    if (!_selectedTemplate.Entries.Any(e => e.SkillID == prereq.Skill.ID && e.Level == lvl))
                    {
                        _selectedTemplate.Entries.Add(new GlobalPlanTemplateEntry
                        {
                            SkillID = prereq.Skill.ID,
                            SkillName = prereq.Skill.Name,
                            Level = lvl
                        });
                        EnsurePrerequisites(prereq.Skill.ID, lvl);
                    }
                }
            }
        }

        public void RemoveSkill(int skillId, int level)
        {
            if (_selectedTemplate == null) return;
            _selectedTemplate.Entries.RemoveAll(e => e.SkillID == skillId && e.Level == level);
            Settings.Save();
            BuildComparisonRows();
        }

        public GlobalPlanTemplate CreateFromPlan(Plan plan)
        {
            var template = new GlobalPlanTemplate
            {
                Name = plan.Name,
            };

            foreach (var entry in plan)
            {
                if (!template.Entries.Any(e => e.SkillID == entry.Skill.ID && e.Level == (int)entry.Level))
                {
                    template.Entries.Add(new GlobalPlanTemplateEntry
                    {
                        SkillID = entry.Skill.ID,
                        SkillName = entry.Skill.Name,
                        Level = (int)entry.Level
                    });
                }
            }

            _templates.Add(template);
            Settings.GlobalPlanTemplates.Add(template);
            Settings.Save();
            return template;
        }

        #endregion

        #region Character Subscription

        public void SubscribeCharacter(Character character)
        {
            if (_selectedTemplate == null) return;
            if (_selectedTemplate.SubscribedCharacterGuids.Contains(character.Guid)) return;

            _selectedTemplate.SubscribedCharacterGuids.Add(character.Guid);
            Settings.Save();
            ResolveSubscribedCharacters();
            BuildComparisonRows();
        }

        public void UnsubscribeCharacter(Character character)
        {
            if (_selectedTemplate == null) return;
            _selectedTemplate.SubscribedCharacterGuids.Remove(character.Guid);
            Settings.Save();
            ResolveSubscribedCharacters();
            BuildComparisonRows();
        }

        public void GeneratePersonalPlan(Character character)
        {
            if (_selectedTemplate == null || character is not CCPCharacter ccp) return;

            string planName = $"{_selectedTemplate.Name}";
            int suffix = 1;
            while (ccp.Plans.Any(p => p.Name == planName))
                planName = $"{_selectedTemplate.Name} ({suffix++})";

            var plan = new Plan(ccp) { Name = planName };

            foreach (var entry in _selectedTemplate.Entries.OrderBy(e => e.SkillID).ThenBy(e => e.Level))
            {
                var skill = StaticSkills.GetSkillByID(entry.SkillID);
                if (skill == null) continue;
                if (character.GetSkillLevel(skill) >= entry.Level) continue;

                plan.PlanTo(skill, entry.Level);
            }

            ccp.Plans.Add(plan);
        }

        #endregion

        public TimeSpan GetCharacterTotalTime(int characterIndex)
        {
            var total = TimeSpan.Zero;
            foreach (var row in _comparisonRows)
            {
                if (characterIndex < row.CharacterEntries.Count)
                    total += row.CharacterEntries[characterIndex].TrainingTime;
            }
            return total;
        }

        public int GetCharacterTrainedCount(int characterIndex)
        {
            return _comparisonRows.Count(row =>
                characterIndex < row.CharacterEntries.Count &&
                row.CharacterEntries[characterIndex].Status == SkillTrainingStatus.AlreadyTrained);
        }
    }

    public sealed class SkillComparisonRow
    {
        public string SkillName { get; set; } = string.Empty;
        public string SkillGroup { get; set; } = string.Empty;
        public int TargetLevel { get; set; }
        public int Rank { get; set; }
        public string PrimaryAttribute { get; set; } = string.Empty;
        public string SecondaryAttribute { get; set; } = string.Empty;
        public List<CharacterSkillEntry> CharacterEntries { get; set; } = new();
    }

    public sealed class CharacterSkillEntry
    {
        public string CharacterName { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }
        public int TargetLevel { get; set; }
        public TimeSpan TrainingTime { get; set; }
        public int SpPerHour { get; set; }
        public SkillTrainingStatus Status { get; set; }
    }

    public enum SkillTrainingStatus
    {
        NeedsTraining,
        AlreadyTrained,
    }
}
