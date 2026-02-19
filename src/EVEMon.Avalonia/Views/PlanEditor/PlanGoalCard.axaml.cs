using Avalonia.Controls;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanGoalCard : UserControl
    {
        public PlanGoalCard()
        {
            InitializeComponent();
        }

        public void Update(string planName, int totalSkills, int notKnownCount, int uniqueSkillsCount)
        {
            PlanNameText.Text = planName;

            int knownCount = uniqueSkillsCount - notKnownCount;
            double progress = uniqueSkillsCount > 0
                ? (double)knownCount / uniqueSkillsCount * 100
                : 0;

            ProgressBar.Value = progress;
            ProgressText.Text = $"{progress:F0}%";
            SkillCountText.Text = $"{totalSkills} skills ({notKnownCount} to train)";
        }
    }
}
