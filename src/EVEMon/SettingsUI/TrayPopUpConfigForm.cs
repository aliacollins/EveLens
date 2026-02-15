using System;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common.Controls;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Extensions;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.SettingsUI
{
    /// <summary>
    /// Configuration editor for the tray icon popup window
    /// </summary>
    public partial class TrayPopupConfigForm : EVEMonForm
    {
        private readonly object[] m_characterGrouping = {
                                                            "None", "Training / Not Training", "Not Training / Training",
                                                            "Account"
                                                        };

        private readonly string[] m_sortOrder = {
                                                    "Training completion, earliest at bottom",
                                                    "Training completion, earliest at top",
                                                    "Alphabetical, first at top",
                                                    "Alphabetical, first at bottom"
                                                };

        private readonly TrayPopupSettings m_settings = null!;
        private readonly object[] m_portraitSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayPopupConfigForm"/> class.
        /// </summary>
        private TrayPopupConfigForm()
        {
            InitializeComponent();
            m_portraitSize = Enum.GetValues(typeof(PortraitSizes))
                .Cast<PortraitSizes>()
                .Select(
                    portraitSize =>
                    {
                        string size = FormattableString.Invariant($"{portraitSize.GetDefaultValue()}");
                        return $"{size} by {size}";
                    })
                .ToArray<object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrayPopupConfigForm"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public TrayPopupConfigForm(TrayPopupSettings settings)
            : this()
        {
            m_settings = settings;
            cbGroupBy.Items.AddRange(m_characterGrouping);
        }

        /// <summary>
        /// On load, restores the window rectangle from the settings.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (m_settings != null)
                DisplayConfig(m_settings);
        }

        /// <summary>
        /// Displays the config.
        /// </summary>
        /// <param name="config">The config.</param>
        private void DisplayConfig(TrayPopupSettings config)
        {
            cbHideNotTraining.Checked = !config.ShowCharNotTraining;
            cbGroupBy.SelectedIndex = (int)config.GroupBy;
            UpdateDisplayOrders();
            if (cbHideNotTraining.Checked)
                cbDisplayOrder1.SelectedItem = m_sortOrder[(int)config.PrimarySortOrder];
            else
            {
                switch (config.GroupBy)
                {
                    case TrayPopupGrouping.None:
                        cbDisplayOrder1.SelectedItem = m_sortOrder[(int)config.PrimarySortOrder];
                        break;
                    case TrayPopupGrouping.Account:
                        cbDisplayOrder1.SelectedItem = m_sortOrder[(int)config.PrimarySortOrder];
                        break;
                    default:
                        cbDisplayOrder1.SelectedItem = m_sortOrder[(int)config.PrimarySortOrder];
                        cbDisplayOrder2.SelectedItem = m_sortOrder[(int)config.SecondarySortOrder];
                        break;
                }
            }
            cbShowSkill.Checked = config.ShowSkillInTraining;
            cbShowTimeToCompletion.Checked = config.ShowRemainingTime;
            cbShowCompletionTime.Checked = config.ShowCompletionTime;
            cbShowSkillQueueTrainingTime.Checked = config.ShowSkillQueueTrainingTime;
            cbHighLightConflicts.Checked = config.HighlightConflicts;
            cbShowWallet.Checked = config.ShowWallet;
            cbShowPortrait.Checked = config.ShowPortrait;
            cbPortraitSize.Items.AddRange(m_portraitSize);
            cbPortraitSize.SelectedIndex = (int)config.PortraitSize;
            cbShowWarning.Checked = config.ShowWarning;
            cbShowServerStatus.Checked = config.ShowServerStatus;
            cbShowEveTime.Checked = config.ShowEveTime;
            cbIndentGroupedAccounts.Checked = config.IndentGroupedAccounts;
            cbUseIncreasedContrast.Checked = config.UseIncreasedContrast;
            UpdateEnables();
        }

        /// <summary>
        /// Applies to config.
        /// </summary>
        private void ApplyToConfig()
        {
            m_settings.ShowCharNotTraining = !cbHideNotTraining.Checked;
            m_settings.GroupBy = (TrayPopupGrouping)cbGroupBy.SelectedIndex;
            m_settings.PrimarySortOrder = GetSortOrder((cbDisplayOrder1.SelectedItem as string)!);
            m_settings.SecondarySortOrder = GetSortOrder((cbDisplayOrder2.SelectedItem as string)!);
            m_settings.ShowSkillInTraining = cbShowSkill.Checked;
            m_settings.ShowRemainingTime = cbShowTimeToCompletion.Checked;
            m_settings.ShowCompletionTime = cbShowCompletionTime.Checked;
            m_settings.ShowSkillQueueTrainingTime = cbShowSkillQueueTrainingTime.Checked;
            m_settings.HighlightConflicts = cbHighLightConflicts.Checked;
            m_settings.ShowWallet = cbShowWallet.Checked;
            m_settings.ShowPortrait = cbShowPortrait.Checked;
            m_settings.PortraitSize = (PortraitSizes)cbPortraitSize.SelectedIndex;
            m_settings.ShowWarning = cbShowWarning.Checked;
            m_settings.ShowServerStatus = cbShowServerStatus.Checked;
            m_settings.ShowEveTime = cbShowEveTime.Checked;
            m_settings.IndentGroupedAccounts = cbIndentGroupedAccounts.Checked;
            m_settings.UseIncreasedContrast = cbUseIncreasedContrast.Checked;
        }

        /// <summary>
        /// Gets the sort order.
        /// </summary>
        /// <param name="selectedSortOrder">The selected sort order.</param>
        /// <returns></returns>
        private TrayPopupSort GetSortOrder(string selectedSortOrder)
        {
            TrayPopupSort sortOrder = TrayPopupSort.TrainingCompletionTimeDESC;
            for (int i = 0; i < m_sortOrder.Length; i++)
            {
                if (selectedSortOrder == m_sortOrder[i])
                    sortOrder = (TrayPopupSort)i;
            }
            return sortOrder;
        }

        /// <summary>
        /// Enables / Disable control accordingly to dependencies.
        /// </summary>
        private void UpdateEnables()
        {
            cbGroupBy.Enabled = !cbHideNotTraining.Checked;
            cbIndentGroupedAccounts.Enabled = (TrayPopupGrouping)cbGroupBy.SelectedIndex == TrayPopupGrouping.Account;
            cbPortraitSize.Enabled = cbShowPortrait.Checked;
        }

        /// <summary>
        /// Updates the display orders.
        /// </summary>
        private void UpdateDisplayOrders()
        {
            if (cbHideNotTraining.Checked)
            {
                lblDisplayOrder1.Text = "Characters in training:";
                cbDisplayOrder1.Items.Clear();
                cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeDESC]);
                cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeASC]);
                cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.NameASC]);
                cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.NameDESC]);
                cbDisplayOrder1.SelectedItem = m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeDESC];
                lblDisplayOrder2.Visible = false;
                cbDisplayOrder2.Visible = false;
            }
            else
            {
                TrayPopupGrouping groupBy = (TrayPopupGrouping)cbGroupBy.SelectedIndex;
                switch (groupBy)
                {
                    case TrayPopupGrouping.None:
                        lblDisplayOrder1.Text = "All characters:";
                        cbDisplayOrder1.Items.Clear();
                        cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.NameASC]);
                        cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.NameDESC]);
                        cbDisplayOrder1.SelectedItem = m_sortOrder[(int)TrayPopupSort.NameASC];
                        lblDisplayOrder2.Visible = false;
                        cbDisplayOrder2.Visible = false;
                        break;
                    case TrayPopupGrouping.Account:
                        lblDisplayOrder1.Text = "Accounts:";
                        cbDisplayOrder1.Items.Clear();
                        cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeDESC]);
                        cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeASC]);
                        cbDisplayOrder1.SelectedItem = m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeDESC];
                        lblDisplayOrder2.Visible = false;
                        cbDisplayOrder2.Visible = false;
                        break;
                    default:
                        lblDisplayOrder1.Text = "Characters in training:";
                        cbDisplayOrder1.Items.Clear();
                        cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeDESC]);
                        cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeASC]);
                        cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.NameASC]);
                        cbDisplayOrder1.Items.Add(m_sortOrder[(int)TrayPopupSort.NameDESC]);
                        cbDisplayOrder1.SelectedItem = m_sortOrder[(int)TrayPopupSort.TrainingCompletionTimeDESC];
                        lblDisplayOrder2.Text = "Characters not training:";
                        cbDisplayOrder2.Items.Clear();
                        cbDisplayOrder2.Items.Add(m_sortOrder[(int)TrayPopupSort.NameASC]);
                        cbDisplayOrder2.Items.Add(m_sortOrder[(int)TrayPopupSort.NameDESC]);
                        cbDisplayOrder2.SelectedItem = m_sortOrder[(int)TrayPopupSort.NameASC];
                        lblDisplayOrder2.Visible = true;
                        cbDisplayOrder2.Visible = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the btnOK control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnOK_Click(object sender, EventArgs e)
        {
            ApplyToConfig();
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbHideNotTraining control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void cbHideNotTraining_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnables();
            UpdateDisplayOrders();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cbGroupBy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void cbGroupBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEnables();
            UpdateDisplayOrders();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the cbShowPortrait control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void cbShowPortrait_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEnables();
        }

        /// <summary>
        /// Handles the Click event of the btnUseDefaults control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void btnUseDefaults_Click(object sender, EventArgs e)
        {
            DisplayConfig(new TrayPopupSettings());
        }
    }
}