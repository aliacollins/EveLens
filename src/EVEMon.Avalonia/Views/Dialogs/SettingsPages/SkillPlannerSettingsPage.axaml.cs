using Avalonia.Controls;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class SkillPlannerSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public SkillPlannerSettingsPage()
        {
            InitializeComponent();
        }

        public SkillPlannerSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            var pws = _settings.UI.PlanWindow;

            HighlightPlannedSkillsCheckBox.IsChecked = pws.HighlightPlannedSkills;
            HighlightPrerequisitesCheckBox.IsChecked = pws.HighlightPrerequisites;
            HighlightConflictsCheckBox.IsChecked = pws.HighlightConflicts;
            HighlightPartialSkillsCheckBox.IsChecked = pws.HighlightPartialSkills;
            HighlightQueuedSkillsCheckBox.IsChecked = pws.HighlightQueuedSkills;

            SummaryOnMultiSelectCheckBox.IsChecked = pws.OnlyShowSelectionSummaryOnMultiSelect;
            AdvanceEntryAddCheckBox.IsChecked = pws.UseAdvanceEntryAddition;

            AlwaysAskRadio.IsChecked = pws.ObsoleteEntryRemovalBehaviour == ObsoleteEntryRemovalBehaviour.AlwaysAsk;
            RemoveAllRadio.IsChecked = pws.ObsoleteEntryRemovalBehaviour == ObsoleteEntryRemovalBehaviour.RemoveAll;
            RemoveConfirmedRadio.IsChecked = pws.ObsoleteEntryRemovalBehaviour == ObsoleteEntryRemovalBehaviour.RemoveConfirmed;
        }

        public void ApplyToSettings()
        {
            var pws = _settings.UI.PlanWindow;

            pws.HighlightPlannedSkills = HighlightPlannedSkillsCheckBox.IsChecked == true;
            pws.HighlightPrerequisites = HighlightPrerequisitesCheckBox.IsChecked == true;
            pws.HighlightConflicts = HighlightConflictsCheckBox.IsChecked == true;
            pws.HighlightPartialSkills = HighlightPartialSkillsCheckBox.IsChecked == true;
            pws.HighlightQueuedSkills = HighlightQueuedSkillsCheckBox.IsChecked == true;

            pws.OnlyShowSelectionSummaryOnMultiSelect = SummaryOnMultiSelectCheckBox.IsChecked == true;
            pws.UseAdvanceEntryAddition = AdvanceEntryAddCheckBox.IsChecked == true;

            if (AlwaysAskRadio.IsChecked == true)
                pws.ObsoleteEntryRemovalBehaviour = ObsoleteEntryRemovalBehaviour.AlwaysAsk;
            else if (RemoveAllRadio.IsChecked == true)
                pws.ObsoleteEntryRemovalBehaviour = ObsoleteEntryRemovalBehaviour.RemoveAll;
            else
                pws.ObsoleteEntryRemovalBehaviour = ObsoleteEntryRemovalBehaviour.RemoveConfirmed;
        }
    }
}
