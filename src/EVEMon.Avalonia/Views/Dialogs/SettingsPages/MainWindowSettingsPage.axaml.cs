// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Controls;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class MainWindowSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public MainWindowSettingsPage()
        {
            InitializeComponent();
        }

        public MainWindowSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            var mws = _settings.UI.MainWindow;

            // Window title
            ShowCharInfoInTitleCheckBox.IsChecked = mws.ShowCharacterInfoInTitleBar;
            ShowSkillInTitleCheckBox.IsChecked = mws.ShowSkillNameInWindowTitle;

            // Character monitor
            ShowAllPublicSkillsCheckBox.IsChecked = mws.ShowAllPublicSkills;
            ShowNonPublicSkillsCheckBox.IsChecked = mws.ShowNonPublicSkills;
            ShowPrereqMetSkillsCheckBox.IsChecked = mws.ShowPrereqMetSkills;
            HighlightPartialSkillsCheckBox.IsChecked = mws.HighlightPartialSkills;
            HighlightQueuedSkillsCheckBox.IsChecked = mws.HighlightQueuedSkills;
            AlwaysShowSkillQueueTimeCheckBox.IsChecked = mws.AlwaysShowSkillQueueTime;
            SkillQueueWarningDays.Value = mws.SkillQueueWarningThresholdDays;

            // Overview
            ShowOverviewCheckBox.IsChecked = mws.ShowOverview;
            ShowOverviewPortraitCheckBox.IsChecked = mws.ShowOverviewPortrait;
            ShowOverviewWalletCheckBox.IsChecked = mws.ShowOverviewWallet;
            ShowOverviewSkillPointsCheckBox.IsChecked = mws.ShowOverviewTotalSkillpoints;
            ShowOverviewSkillQueueTimeCheckBox.IsChecked = mws.ShowOverviewSkillQueueTrainingTime;
            GroupTrainingFirstCheckBox.IsChecked = mws.PutTrainingSkillsFirstOnOverview;
            IncreasedContrastCheckBox.IsChecked = mws.UseIncreasedContrastOnOverview;
        }

        public void ApplyToSettings()
        {
            var mws = _settings.UI.MainWindow;

            // Window title
            mws.ShowCharacterInfoInTitleBar = ShowCharInfoInTitleCheckBox.IsChecked == true;
            mws.ShowSkillNameInWindowTitle = ShowSkillInTitleCheckBox.IsChecked == true;

            // Character monitor
            mws.ShowAllPublicSkills = ShowAllPublicSkillsCheckBox.IsChecked == true;
            mws.ShowNonPublicSkills = ShowNonPublicSkillsCheckBox.IsChecked == true;
            mws.ShowPrereqMetSkills = ShowPrereqMetSkillsCheckBox.IsChecked == true;
            mws.HighlightPartialSkills = HighlightPartialSkillsCheckBox.IsChecked == true;
            mws.HighlightQueuedSkills = HighlightQueuedSkillsCheckBox.IsChecked == true;
            mws.AlwaysShowSkillQueueTime = AlwaysShowSkillQueueTimeCheckBox.IsChecked == true;
            mws.SkillQueueWarningThresholdDays = (int)(SkillQueueWarningDays.Value ?? 1);

            // Overview
            mws.ShowOverview = ShowOverviewCheckBox.IsChecked == true;
            mws.ShowOverviewPortrait = ShowOverviewPortraitCheckBox.IsChecked == true;
            mws.ShowOverviewWallet = ShowOverviewWalletCheckBox.IsChecked == true;
            mws.ShowOverviewTotalSkillpoints = ShowOverviewSkillPointsCheckBox.IsChecked == true;
            mws.ShowOverviewSkillQueueTrainingTime = ShowOverviewSkillQueueTimeCheckBox.IsChecked == true;
            mws.PutTrainingSkillsFirstOnOverview = GroupTrainingFirstCheckBox.IsChecked == true;
            mws.UseIncreasedContrastOnOverview = IncreasedContrastCheckBox.IsChecked == true;
        }
    }
}
