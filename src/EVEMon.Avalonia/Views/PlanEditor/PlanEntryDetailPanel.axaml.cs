using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using EVEMon.Common.Data;
using EVEMon.Common.Models;
using EVEMon.Avalonia.ViewModels;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanEntryDetailPanel : UserControl
    {
        public PlanEntryDetailPanel()
        {
            InitializeComponent();
        }

        internal void ShowEntry(PlanEntryDisplayItem? item, Character? character)
        {
            if (item == null)
            {
                DetailBorder.IsVisible = false;
                return;
            }

            DetailBorder.IsVisible = true;
            PlanEntry entry = item.Entry;
            StaticSkill skill = entry.Skill;

            DetailSkillName.Text = item.SkillName;
            DetailDescription.Text = skill.Description;
            DetailPrimary.Text = skill.PrimaryAttribute.ToString();
            DetailSecondary.Text = skill.SecondaryAttribute.ToString();
            DetailTrainingTime.Text = item.TrainingTimeText;
            DetailSpPerHour.Text = item.SpPerHourText;

            var prereqs = BuildPrereqList(skill, character);
            PrereqList.ItemsSource = prereqs;
        }

        public void Hide()
        {
            DetailBorder.IsVisible = false;
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
