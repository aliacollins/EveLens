// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EVEMon.Common.Data;
using EVEMon.Common.Models;
using EVEMon.Avalonia.ViewModels;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanEntryDetailPanel : UserControl
    {
        private PlanEntry? _currentEntry;

        public PlanEntryDetailPanel()
        {
            InitializeComponent();
            NotesTextBox.LostFocus += OnNotesLostFocus;
        }

        internal void ShowEntry(PlanEntryDisplayItem? item, Character? character)
        {
            // Save notes from previous entry before switching
            SaveCurrentNotes();

            if (item == null)
            {
                _currentEntry = null;
                DetailBorder.IsVisible = false;
                return;
            }

            DetailBorder.IsVisible = true;
            PlanEntry entry = item.Entry;
            _currentEntry = entry;
            StaticSkill skill = entry.Skill;

            DetailSkillName.Text = item.SkillName;
            DetailDescription.Text = skill.Description;
            DetailPrimary.Text = skill.PrimaryAttribute.ToString();
            DetailSecondary.Text = skill.SecondaryAttribute.ToString();
            DetailTrainingTime.Text = item.TrainingTimeText;
            DetailSpPerHour.Text = item.SpPerHourText;

            var prereqs = BuildPrereqList(skill, character);
            PrereqList.ItemsSource = prereqs;

            // Load notes
            NotesTextBox.Text = entry.Notes ?? string.Empty;
        }

        public void Hide()
        {
            SaveCurrentNotes();
            _currentEntry = null;
            DetailBorder.IsVisible = false;
        }

        private void OnNotesLostFocus(object? sender, RoutedEventArgs e)
        {
            SaveCurrentNotes();
        }

        private void SaveCurrentNotes()
        {
            if (_currentEntry == null) return;
            string newNotes = NotesTextBox.Text ?? string.Empty;
            if (_currentEntry.Notes != newNotes)
            {
                _currentEntry.Notes = newNotes;
            }
        }

        private static List<PrereqDisplayItem> BuildPrereqList(StaticSkill skill, Character? character)
        {
            return skill.Prerequisites.Select(prereq =>
            {
                bool isTrained = character != null && character.GetSkillLevel(prereq.Skill) >= prereq.Level;
                string text = $"{prereq.Skill.Name} {Skill.GetRomanFromInt(prereq.Level)}";
                return new PrereqDisplayItem(text, isTrained);
            }).ToList();
        }
    }

    internal sealed class PrereqDisplayItem
    {
        private static readonly IBrush TrainedBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush MissingBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));

        public string Text { get; }
        public string StatusIcon { get; }
        public IBrush TextBrush { get; }

        public PrereqDisplayItem(string text, bool isTrained)
        {
            Text = text;
            StatusIcon = isTrained ? "\u2713" : "\u2717";
            TextBrush = isTrained ? TrainedBrush : MissingBrush;
        }
    }
}
