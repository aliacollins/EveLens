using System;
using System.Windows.Forms;
using EVEMon.Common.Controls;

namespace EVEMon.SkillPlanner
{
    public partial class BoosterInjectionWindow : EVEMonForm
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoosterInjectionWindow"/> class.
        /// </summary>
        public BoosterInjectionWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the selected booster bonus.
        /// </summary>
        public int BoosterBonus { get; private set; }

        /// <summary>
        /// Gets the duration in hours.
        /// </summary>
        public int DurationHours { get; private set; }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the btnOk control.
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            BoosterBonus = (int)cbBoosterBonus.SelectedItem;
            DurationHours = (int)nudDuration.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Handles the Shown event of the window.
        /// </summary>
        private void BoosterInjectionWindow_Shown(object sender, EventArgs e)
        {
            // Populate booster bonus options
            cbBoosterBonus.Items.Clear();
            cbBoosterBonus.Items.Add(3);   // Basic accelerator
            cbBoosterBonus.Items.Add(6);   // Standard accelerator
            cbBoosterBonus.Items.Add(8);   // Advanced accelerator
            cbBoosterBonus.Items.Add(10);  // Extended accelerator
            cbBoosterBonus.Items.Add(12);  // Master-at-Arms accelerator
            cbBoosterBonus.SelectedIndex = 2; // Default to +8

            nudDuration.Value = 24; // Default 24 hours
        }

        /// <summary>
        /// Handles the Format event for the booster bonus combo box.
        /// </summary>
        private void cbBoosterBonus_Format(object sender, ListControlConvertEventArgs e)
        {
            if (e.ListItem is int bonus)
            {
                e.Value = $"+{bonus} to all attributes";
            }
        }
    }
}
