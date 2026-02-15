using System;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.Common.Models;
using EVEMon.Common.Services;

namespace EVEMon.SkillPlanner
{
    /// <summary>
    /// This window allow the exportation of plans between characters.
    /// </summary>
    public partial class PlanImportationFromCharacterWindow : EVEMonForm
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetCharacter"></param>
        public PlanImportationFromCharacterWindow(Character targetCharacter)
        {
            InitializeComponent();
            TargetCharacter = targetCharacter;
        }

        /// <summary>
        /// Gets the selected source plan.
        /// </summary>
        public Plan? SourcePlan { get; private set; }

        /// <summary>
        /// Gets the target character.
        /// </summary>
        public Character TargetCharacter { get; }

        /// <summary>
        /// Gets the exported plan. 
        /// </summary>
        /// <remarks>This plan has not been added to the character's list and still has the same name than the source plan.</remarks>
        public Plan? TargetPlan { get; private set; }

        /// <summary>
        /// Populate the plans list from the given character
        /// </summary>
        /// <param name="character"></param>
        private void PopulatePlans(Character character)
        {
            btnLoad.Enabled = false;
            lbPlan.Items.Clear();
            foreach (Plan plan in character.Plans)
            {
                lbPlan.Items.Add(plan);
            }
        }


        #region Event handlers

        /// <summary>
        /// Populate the character list with all characters except the target
        /// </summary>
        private void CrossPlanSelect_Load(object? sender,EventArgs e)
        {
            cbCharacter.Items.Clear();
            foreach (Character character in AppServices.Characters.Where(x => x.CharacterID != TargetCharacter.CharacterID))
            {
                cbCharacter.Items.Add(character);
            }
            cbCharacter.SelectedIndex = 0;
            PopulatePlans((cbCharacter.SelectedItem as Character)!);
        }

        /// <summary>
        /// When the selected character changed, we update the plans list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbCharacter_SelectedIndexChanged(object? sender,EventArgs e)
        {
            PopulatePlans((cbCharacter.Items[cbCharacter.SelectedIndex] as Character)!);
        }

        /// <summary>
        /// When the selected plan changed, we enable/disable the "load" button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbPlan_SelectedIndexChanged(object? sender,EventArgs e)
        {
            btnLoad.Enabled = lbPlan.SelectedItems.Count == 1;
        }

        /// <summary>
        /// When the user clicks "load", import the plan.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoad_Click(object? sender,EventArgs e)
        {
            SourcePlan = (Plan)lbPlan.SelectedItem!;
            TargetPlan = SourcePlan.Clone(TargetCharacter);
            DialogResult = DialogResult.OK;
            Close();
        }

        #endregion
    }
}