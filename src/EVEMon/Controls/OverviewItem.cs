// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using EVEMon.Common;
using EVEMon.Common.Constants;
using EVEMon.Common.Controls;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Data;
using EVEMon.Common.Helpers;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Extensions;
using EVEMon.Common.Factories;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Scheduling;
using EVEMon.Common.Services;
using EVEMon.Common.Events;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Controls
{
    /// <summary>
    /// Represents an item displayed on the overview.
    /// </summary>
    public partial class OverviewItem : UserControl
    {
        /// <summary>
        /// Approximately how long to display each component of the revolving industry jobs.
        /// </summary>
        private const int INTERVAL = 5;

        #region Fields

        private readonly bool m_isTooltip;

        private Color m_settingsForeColor;
        private bool m_showConflicts;
        private bool m_showSkillInTraining;
        private bool m_showCompletionTime;
        private bool m_showRemainingTime;
        private bool m_showWalletBalance;

        // While an enum for the type of info to show would be more scalable, these booleans
        // exist separately for compatibility with earlier versions of EVEMon 4
        private bool m_showJobs;
        private bool m_showLocation;

        private bool m_showSkillpoints;
        private bool m_showPortrait;
        private bool m_showSkillQueueTrainingTime;
        private int m_portraitSize;

        private bool m_hovered;
        private bool m_pressed;
        private int m_preferredWidth;
        private int m_preferredHeight;
        private int m_minWidth;

        private bool m_hasRemainingTime;
        private bool m_hasCompletionTime;
        private bool m_hasSkillInTraining;
        private bool m_hasSkillQueueTrainingTime;

        private float m_regularFontSize;
        private float m_mediumFontSize;
        private float m_bigFontSize;

        private IDisposable? _subSkillQueuesBatchUpdated;
        private IDisposable? _subQueuedSkillsCompleted;
        private IDisposable? _subMarketOrdersUpdated;
        private IDisposable? _subCharactersBatchUpdated;
        private IDisposable? _subSchedulerChanged;
        private IDisposable? _subSettingsChanged;
        private IDisposable? _subCharacterLabelChanged;
        private IDisposable? _subESIKeyInfoUpdated;
        private IDisposable? _subSecondTick;

        #endregion


        #region Constructors

        /// <summary>
        /// Default constructor for designer.
        /// </summary>
        private OverviewItem()
        {
            m_portraitSize = 96;
            m_preferredHeight = 1;
            m_preferredWidth = 1;
            
            InitializeComponent();
        }

        /// <summary>
        /// Constructor used in-code.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="isTooltip">if set to <c>true</c> if this instance is used as tooltip.</param>
        internal OverviewItem(Character character, bool isTooltip = false)
            : this()
        {
            m_isTooltip = isTooltip;
            Character = character;
        }

        #endregion


        #region Inherited Events

        /// <summary>
        /// Completes initialization.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Returns in design mode or when no char
            if (DesignMode || this.IsDesignModeHosted())
                return;

            DoubleBuffered = true;

            // Font sizes
            m_regularFontSize = 8.25F;
            m_mediumFontSize = 9.75F;
            m_bigFontSize = 11.25F;

            // Set base font for the control (all child labels inherit this)
            Font = FontFactory.GetFont("Tahoma");

            // Initializes fonts
            lblCharName.Font = FontFactory.GetFont("Tahoma", m_bigFontSize, FontStyle.Bold);
            lblBalance.Font = FontFactory.GetFont("Tahoma", m_mediumFontSize, FontStyle.Bold);
            lblRemainingTime.Font = FontFactory.GetFont("Tahoma", m_mediumFontSize);
            lblSkillInTraining.Font = FontFactory.GetFont("Tahoma", m_regularFontSize);
            lblCompletionTime.Font = FontFactory.GetFont("Tahoma", m_regularFontSize);
            lblSkillQueueTrainingTime.Font = FontFactory.GetFont("Tahoma", m_regularFontSize);
            lblExtraInfo.Font = FontFactory.GetFont("Tahoma", m_regularFontSize);
            lblTotalSkillPoints.Font = FontFactory.GetFont("Tahoma", m_mediumFontSize, FontStyle.Bold);
            lblESIKeyWarning.Font = FontFactory.GetFont("Tahoma", m_regularFontSize, FontStyle.Bold);

            // Initializes the portrait
            pbCharacterPortrait.Hide();
            pbCharacterPortrait.Character = Character;

            // Initialize the skill queue training time label text
            lblSkillQueueTrainingTime.Text = string.Empty;

            // Global events
            _subSkillQueuesBatchUpdated = AppServices.EventAggregator.SubscribeOnUIForCharacterBatch<SkillQueuesBatchUpdatedEvent>(this, () => Character, OnSkillQueuesBatchUpdated);
            _subQueuedSkillsCompleted = AppServices.EventAggregator.SubscribeOnUIForCharacter<QueuedSkillsCompletedEvent>(this, () => Character, OnQueuedSkillsCompleted);
            _subMarketOrdersUpdated = AppServices.EventAggregator.SubscribeOnUIForCharacter<MarketOrdersUpdatedEvent>(this, () => Character, OnMarketOrdersUpdated);
            _subCharactersBatchUpdated = AppServices.EventAggregator.SubscribeOnUIForCharacterBatch<CharactersBatchUpdatedEvent>(this, () => Character, OnCharactersBatchUpdated);
            _subSchedulerChanged = AppServices.EventAggregator.SubscribeOnUI<SchedulerChangedEvent>(this, OnSchedulerChanged);
            _subSettingsChanged = AppServices.EventAggregator.SubscribeOnUI<SettingsChangedEvent>(this, OnSettingsChanged);
            // Use FiveSecondTickEvent instead of SecondTickEvent to reduce handler count from 100/sec to ~20/5sec
            // for 100+ characters. Overview shows "2d 14h" not "2d 14h 32s" so 5-second granularity is sufficient.
            _subSecondTick = AppServices.EventAggregator.SubscribeOnUI<EVEMon.Core.Events.FiveSecondTickEvent>(this, _ => EveMonClient_TimerTick(null, EventArgs.Empty));
            _subCharacterLabelChanged = AppServices.EventAggregator.SubscribeOnUI<CharacterLabelChangedEvent>(this, OnCharacterLabelChanged);
            _subESIKeyInfoUpdated = AppServices.EventAggregator.SubscribeOnUI<ESIKeyInfoUpdatedEvent>(this, OnESIKeyInfoUpdated);
            Disposed += OnDisposed;

            UpdateOnSettingsChanged();
        }

        /// <summary>
        /// On dispose, unsubscribe events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDisposed(object? sender, EventArgs e)
        {
            _subSkillQueuesBatchUpdated?.Dispose();
            _subQueuedSkillsCompleted?.Dispose();
            _subMarketOrdersUpdated?.Dispose();
            _subCharactersBatchUpdated?.Dispose();
            _subSchedulerChanged?.Dispose();
            _subSettingsChanged?.Dispose();
            _subSecondTick?.Dispose();
            _subCharacterLabelChanged?.Dispose();
            _subESIKeyInfoUpdated?.Dispose();
            Disposed -= OnDisposed;
        }

        /// <summary>
        /// Occurs when the visibility changed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible)
            {
                UpdateContent();
                UpdateTrainingTime();
            }
        }

        /// <summary>
        /// Paints a button behind when hovered.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!m_hovered)
                return;

            ButtonRenderer.DrawButton(e.Graphics, DisplayRectangle, m_pressed ?
                PushButtonState.Pressed : PushButtonState.Hot);
        }

        /// <summary>
        /// When the mouse enters control, we need to display the back button.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            if (!Clickable)
                return;

            // Show back button
            m_hovered = true;
            Invalidate();
        }

        /// <summary>
        /// When the mouse leaves the control, we need to hide the button background.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            m_hovered = false;
            Invalidate();
        }

        /// <summary>
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            m_pressed = true;
            Invalidate();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            m_pressed = false;
            Invalidate();
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the character control is bound to.
        /// </summary>
        public Character Character { get; } = null!;

        /// <summary>
        /// Gets or sets true whether a button should appear on hover.
        /// </summary>
        [Description("When true, a background button will appear on hover and the control will fire Click event")]
        public bool Clickable { get; set; }

        #endregion


        #region Content update

        /// <summary>
        /// Updates when settings changed.
        /// </summary>
        internal void UpdateOnSettingsChanged()
        {
            TrayPopupSettings trayPopupSettings = Settings.UI.SystemTrayPopup;
            MainWindowSettings mainWindowSettings = Settings.UI.MainWindow;
            PortraitSizes portraitSize = m_isTooltip ? trayPopupSettings.PortraitSize :
                mainWindowSettings.OverviewItemSize;

            // Misc fields
            m_portraitSize = portraitSize.GetDefaultValue();
            m_showConflicts = !m_isTooltip || trayPopupSettings.HighlightConflicts;
            m_showCompletionTime = !m_isTooltip || trayPopupSettings.ShowCompletionTime;
            m_showRemainingTime = !m_isTooltip || trayPopupSettings.ShowRemainingTime;
            m_showSkillInTraining = !m_isTooltip || trayPopupSettings.ShowSkillInTraining;
            m_showWalletBalance = m_isTooltip ? trayPopupSettings.ShowWallet :
                mainWindowSettings.ShowOverviewWallet;
            m_showSkillpoints = !m_isTooltip && mainWindowSettings.
                ShowOverviewTotalSkillpoints;
            m_showPortrait = m_isTooltip ? trayPopupSettings.ShowPortrait :
                mainWindowSettings.ShowOverviewPortrait;
            m_showSkillQueueTrainingTime = m_isTooltip ? trayPopupSettings.
                ShowSkillQueueTrainingTime : mainWindowSettings.
                ShowOverviewSkillQueueTrainingTime;
            m_showLocation = !m_isTooltip && mainWindowSettings.ShowOverviewLocation;
            m_showJobs = !m_isTooltip && mainWindowSettings.ShowOverviewJobs;

            // Update colors
            UpdateContrastColor();

            // Update the controls
            UpdateContent();
        }

        /// <summary>
        /// Updates the color of the contrast.
        /// </summary>
        private void UpdateContrastColor()
        {
            m_settingsForeColor = (m_isTooltip && Settings.UI.SystemTrayPopup.
                UseIncreasedContrast) || (!m_isTooltip && Settings.UI.MainWindow.
                UseIncreasedContrastOnOverview) ? Color.Black : Color.DimGray;

            lblBalance.ForeColor = m_settingsForeColor;
            lblTotalSkillPoints.ForeColor = m_settingsForeColor;
            lblRemainingTime.ForeColor = m_settingsForeColor;
            lblSkillInTraining.ForeColor = m_settingsForeColor;
            lblCompletionTime.ForeColor = m_settingsForeColor;
        }

        /// <summary>
        /// Update the controls.
        /// </summary>
        private void UpdateContent()
        {
            if (!Visible)
                return;

            // Update character's 'Adorned Name' and 'Portrait' in case they have changed
            lblCharName.Text = Character.LabelPrefix + Character.AdornedName;
            pbCharacterPortrait.Character = Character;

            // Show loading placeholders only for characters with genuinely no data
            // (newly added via SSO, never synced). Characters loaded from settings
            // have cached data from the last session — show it immediately.
            bool hasNoData = Character.SkillPoints == 0 && Character.Balance == 0;

            lblTotalSkillPoints.Text = hasNoData
                ? "Fetching..."
                : string.Format("{0:N0} SP", Character.SkillPoints);

            if (hasNoData)
            {
                lblBalance.Text = "Fetching...";
                lblBalance.ForeColor = m_settingsForeColor;
            }
            else
                FormatBalance();

            var ccpCharacter = Character as CCPCharacter;
            QueuedSkill? trainingSkill = Character.CurrentlyTrainingSkill;
            // Character in training ? We have labels to fill
            if (Character.IsTraining || (ccpCharacter != null && trainingSkill != null &&
                ccpCharacter.SkillQueue.IsPaused))
            {
                // Update the skill in training label
                lblSkillInTraining.Text = trainingSkill!.ToString();
                DateTime endTime = trainingSkill.EndTime.ToLocalTime();

                // Updates the time remaining label
                lblRemainingTime.Text = (ccpCharacter != null && ccpCharacter.SkillQueue.
                    IsPaused) ? "Paused" : trainingSkill.RemainingTime.ToDescriptiveText(
                    DescriptiveTextOptions.IncludeCommas);

                // Update the completion time
                lblCompletionTime.Text = (ccpCharacter != null && ccpCharacter.SkillQueue.
                    IsPaused) ? string.Empty : $"{endTime:ddd} {endTime:G}";

                // Changes the completion time color on scheduling block
                string blockingEntry;
                bool isAutoBlocking;
                bool isBlocking = Scheduler.SkillIsBlockedAt(endTime, out blockingEntry,
                    out isAutoBlocking);
                lblCompletionTime.ForeColor = (m_showConflicts && isBlocking &&
                    (ccpCharacter == null || ccpCharacter.SkillQueue.Count == 1 ||
                    !isAutoBlocking)) ? Color.Red : m_settingsForeColor;

                // Update the skill queue training time label
                UpdateSkillQueueTrainingTime();

                // Show the training labels
                m_hasSkillInTraining = true;
                m_hasCompletionTime = true;
                m_hasRemainingTime = true;
                m_hasSkillQueueTrainingTime = true;
            }
            else
            {
                // Hide the training labels
                m_hasSkillInTraining = false;
                m_hasCompletionTime = false;
                m_hasRemainingTime = false;
                m_hasSkillQueueTrainingTime = false;
            }
            UpdateExtraData();
            UpdateESIKeyWarning();
            // Adjusts all the controls layout
            PerformCustomLayout(m_isTooltip);
        }

        /// <summary>
        /// Updates the ESI key warning indicator.
        /// </summary>
        private void UpdateESIKeyWarning()
        {
            var ccpCharacter = Character as CCPCharacter;
            if (ccpCharacter == null)
            {
                lblESIKeyWarning.Visible = false;
                return;
            }

            bool hasLinkedKeys = ccpCharacter.Identity.ESIKeys.Any();
            bool hasKeyError = ccpCharacter.Identity.ESIKeys.Any(key => key.HasError);

            if (!hasLinkedKeys)
            {
                // Key not linked to identity yet — check for unlinked keys in global
                // collection. ESIKey.ID may not match CharacterID (XML migration stores
                // the old key ID, not the character ID), so we look for keys that haven't
                // been associated with any character identity yet.
                var unlinkedKeys = AppServices.ESIKeys
                    .Where(k => !k.CharacterIdentities.Any());
                if (unlinkedKeys.Any(k => k.HasError))
                {
                    // Auth failed — token is stale/invalid
                    lblESIKeyWarning.Text = "Re-auth Required";
                    lblESIKeyWarning.ForeColor = Color.OrangeRed;
                }
                else if (unlinkedKeys.Any(k => !string.IsNullOrEmpty(k.RefreshToken)))
                {
                    // Has token, authentication in progress
                    lblESIKeyWarning.Text = "Connecting...";
                    lblESIKeyWarning.ForeColor = Color.DimGray;
                }
                else if (unlinkedKeys.Any())
                {
                    // Keys exist but tokens are empty — needs re-auth
                    lblESIKeyWarning.Text = "Re-auth Required";
                    lblESIKeyWarning.ForeColor = Color.OrangeRed;
                }
                else
                {
                    // No unlinked keys at all
                    lblESIKeyWarning.Text = "No API Key";
                    lblESIKeyWarning.ForeColor = Color.OrangeRed;
                }
                lblESIKeyWarning.Visible = true;
            }
            else if (hasKeyError)
            {
                lblESIKeyWarning.Text = "API Key Error";
                lblESIKeyWarning.ForeColor = Color.OrangeRed;
                lblESIKeyWarning.Visible = true;
            }
            else
            {
                lblESIKeyWarning.Visible = false;
            }
        }

        /// <summary>
        /// Updates the extra data shown on screen.
        /// </summary>
        private void UpdateExtraData()
        {
            string extraText = string.Empty;
            var ccpCharacter = Character as CCPCharacter;
            if (m_showLocation)
            {
                // Determine the character's system location
                int locID = Character?.LastKnownLocation?.SolarSystemID ?? 0;
                if (locID == 0)
                    extraText = EveMonConstants.UnknownText + " Location";
                else
                    extraText = StaticGeography.GetSolarSystemName(locID);
            }
            else if (m_showJobs && ccpCharacter != null)
            {
                int jobs, max, indJobs = 0, resJobs = 0, reaJobs = 0;
                string desc;
                // Sum up by type
                foreach (var job in ccpCharacter.CharacterIndustryJobs)
                    switch (job.Activity)
                    {
                    case BlueprintActivity.Reactions:
                    case BlueprintActivity.SimpleReactions:
                        reaJobs++;
                        break;
                    case BlueprintActivity.Manufacturing:
                        indJobs++;
                        break;
                    case BlueprintActivity.Copying:
                    case BlueprintActivity.Duplicating:
                    case BlueprintActivity.Invention:
                    case BlueprintActivity.ResearchingMaterialEfficiency:
                    case BlueprintActivity.ResearchingTechnology:
                    case BlueprintActivity.ResearchingTimeEfficiency:
                    case BlueprintActivity.ReverseEngineering:
                        resJobs++;
                        break;
                    default:
                        break;
                    }
                // Determine the character's jobs remaining (character only)
                switch ((DateTime.UtcNow.Second / INTERVAL) % 3)
                {
                case 0:
                default:
                    // Industry
                    max = IndustryJob.MaxManufacturingJobsFor(ccpCharacter);
                    jobs = indJobs;
                    desc = "Indus";
                    break;
                case 1:
                    // Research
                    max = IndustryJob.MaxResearchJobsFor(ccpCharacter);
                    jobs = resJobs;
                    desc = "Resea";
                    break;
                case 2:
                    // Reaction
                    max = IndustryJob.MaxReactionJobsFor(ccpCharacter);
                    jobs = reaJobs;
                    desc = "React";
                    break;
                }
                extraText = string.Format("{0:D} / {1:D} {2}", jobs, max, desc);
            }
            lblExtraInfo.Text = extraText;
        }

        /// <summary>
        /// Formats the balance.
        /// </summary>
        private void FormatBalance()
        {
            lblBalance.Text = $"{Character.Balance:N} ISK";

            CCPCharacter? ccpCharacter = Character as CCPCharacter;
            Color balanceColor = m_settingsForeColor;
            if (ccpCharacter != null && !Settings.UI.SafeForWork)
            {
                IQueryMonitor? marketMonitor = ccpCharacter.QueryMonitors[
                    ESIAPICharacterMethods.MarketOrders];
                // Orange if orders could fail on margin
                if (!ccpCharacter.HasSufficientBalance && marketMonitor != null &&
                        marketMonitor.Enabled)
                    balanceColor = Color.Orange;
                else if (ccpCharacter.Balance < 0)
                    // Red if negative wallet
                    balanceColor = Color.Red;
            }
            lblBalance.ForeColor = balanceColor;
        }

        /// <summary>
        /// Updates the controls' visibility.
        /// </summary>
        /// <returns></returns>
        private void UpdateVisibilities()
        {
            lblRemainingTime.Visible = m_hasRemainingTime && m_showRemainingTime;
            lblCompletionTime.Visible = m_hasCompletionTime && m_showCompletionTime;
            lblSkillInTraining.Visible = m_hasSkillInTraining && m_showSkillInTraining;
            lblSkillQueueTrainingTime.Visible = m_hasSkillQueueTrainingTime &&
                m_showSkillQueueTrainingTime;
            lblBalance.Visible = m_showWalletBalance;
            lblTotalSkillPoints.Visible = m_showSkillpoints;
            lblExtraInfo.Visible = m_showLocation || m_showJobs;
        }

        /// <summary>
        /// Updates the training time.
        /// </summary>
        private void UpdateTrainingTime()
        {
            if (Character.IsTraining)
            {
                TimeSpan remainingTime = Character.CurrentlyTrainingSkill!.RemainingTime;
                lblRemainingTime.Text = remainingTime.ToDescriptiveText(DescriptiveTextOptions.
                    IncludeCommas);
                UpdateSkillQueueTrainingTime();
            }
        }

        /// <summary>
        /// Updates the skill queue training time.
        /// </summary>
        /// <returns></returns>
        private void UpdateSkillQueueTrainingTime()
        {
            CCPCharacter? ccpCharacter = Character as CCPCharacter;
            lblSkillQueueTrainingTime.ForeColor = m_settingsForeColor;
            string text = string.Empty;
            // Current character isn't a CCP character, so can't have a Queue
            if (ccpCharacter != null && !ccpCharacter.SkillQueue.IsPaused)
            {
                TimeSpan skillQueueEndTime = ccpCharacter.SkillQueue.EndTime.Subtract(
                    DateTime.UtcNow);
                TimeSpan timeLeft = SkillQueue.WarningThresholdTimeSpan.Subtract(
                    skillQueueEndTime);

                // Negative time: Skill queue is populated with more than a day
                if (timeLeft < TimeSpan.Zero)
                {
                    // More than one entry in queue ? Display total queue remaining time
                    if (ccpCharacter.SkillQueue.Count > 1)
                    {
                        text = "Queue ends in " + ccpCharacter.SkillQueue.EndTime.
                            ToRemainingTimeShortDescription(DateTimeKind.Utc);
                    }
                }
                // Skill queue is empty ?
                else if (timeLeft > SkillQueue.WarningThresholdTimeSpan)
                {
                    lblSkillQueueTrainingTime.ForeColor = Color.Red;
                    text = "Skill queue is empty";
                }
                else if (timeLeft != TimeSpan.Zero)
                {
                    // Less than one minute? Display seconds else display time without seconds
                    string endTimeText = skillQueueEndTime.ToDescriptiveText(
                        DescriptiveTextOptions.IncludeCommas, skillQueueEndTime < TimeSpan.
                        FromMinutes(1));
                    lblSkillQueueTrainingTime.ForeColor = Color.Red;
                    text = $"Queue ends in {endTimeText}";
                }
            }
            lblSkillQueueTrainingTime.Text = text;
        }

        #endregion


        #region Global Events

        /// <summary>
        /// When a character label is changed, we update if it is our character.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCharacterLabelChanged(CharacterLabelChangedEvent e)
        {
            if (e.Character == Character)
                UpdateContent();
        }

        /// <summary>
        /// When ESI key info is updated, refresh the warning indicator.
        /// </summary>
        private void OnESIKeyInfoUpdated(ESIKeyInfoUpdatedEvent e)
        {
            if (Visible)
                UpdateESIKeyWarning();
        }

        /// <summary>
        /// On every second, we update the remaining time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EveMonClient_TimerTick(object? sender, EventArgs e)
        {
            if (Visible)
            {
                UpdateTrainingTime();
                UpdateExtraData();
            }
        }

        /// <summary>
        /// When the scheduler changed, we may have to display a warning (blocking entry).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSchedulerChanged(SchedulerChangedEvent e)
        {
            UpdateContent();
        }

        /// <summary>
        /// When the settings changed, update if necessary.
        /// </summary>
        private void OnSettingsChanged(SettingsChangedEvent e)
        {
            UpdateOnSettingsChanged();
        }

        /// <summary>
        /// On skill completion.
        /// </summary>
        private void OnQueuedSkillsCompleted(QueuedSkillsCompletedEvent e)
        {
            // Character still training ? Jump to next skill
            if (Character.IsTraining)
                UpdateContent();
            else
            {
                lblRemainingTime.Text = @"Completed";
                m_hasCompletionTime = false;
                UpdateVisibilities();
            }
        }

        /// <summary>
        /// On character market orders updated, update the balance format.
        /// </summary>
        private void OnMarketOrdersUpdated(MarketOrdersUpdatedEvent e)
        {
            FormatBalance();
        }

        /// <summary>
        /// On character sheet changed, update everything.
        /// </summary>
        private void OnCharactersBatchUpdated(CharactersBatchUpdatedEvent e)
        {
            UpdateContent();
        }

        /// <summary>
        /// On character skill queue batch changed, update everything.
        /// </summary>
        private void OnSkillQueuesBatchUpdated(SkillQueuesBatchUpdatedEvent e)
        {
            UpdateContent();
        }

        #endregion


        #region Layout

        /// <summary>
        /// Adjusts all the controls layout.
        /// </summary>
        /// <param name="tooltip"></param>
        private void PerformCustomLayout(bool tooltip)
        {
            int x48 = PortraitSizes.x48.GetDefaultValue(), x64 = PortraitSizes.x64.
                GetDefaultValue(), x80 = PortraitSizes.x80.GetDefaultValue();

            if (!Visible)
                return;
            SuspendLayout();
            UpdateVisibilities();

            bool showPortrait = m_showPortrait && !Settings.UI.SafeForWork;
            int portraitSize = m_portraitSize, margin = 10, smallLabelHeight = 13, labelWidth;
            if (tooltip)
                margin = portraitSize <= x48 ? 2 : (portraitSize <= x64 ? 4 : (portraitSize <=
                    x80 ? 6 : margin));

            // Label height
            int labelHeight = portraitSize <= x48 ? smallLabelHeight : (portraitSize <= x64 ?
                16 : 18);
            // Label width
            if (tooltip)
                labelWidth = 0;
            else
                // Ensure that the graphics is thrown away when used
                using (var g = Graphics.FromHwnd(Handle))
                {
                    labelWidth = (int)(GetMinimumWidth(g) * g.DpiX / EveMonConstants.
                        DefaultDpi);
                }

            // Big font size
            float bigFontSize = portraitSize <= x48 ? m_regularFontSize : (portraitSize <=
                x64 ? m_mediumFontSize : m_bigFontSize);
            // Medium font size
            float mediumFontSize = portraitSize <= x64 ? m_regularFontSize : m_mediumFontSize;
            // Margin between the two labels groups
            int verticalMargin = m_showSkillQueueTrainingTime ? 4 : 16;
            if (portraitSize <= x80)
                verticalMargin = 0;

            // Adjust portrait
            pbCharacterPortrait.Location = new Point(margin, margin);
            pbCharacterPortrait.Size = new Size(portraitSize, portraitSize);
            pbCharacterPortrait.Visible = showPortrait;
            // Adjust the top labels
            int top = margin - 2;
            int left = showPortrait ? portraitSize + margin * 2 : margin;
            int rightPad = tooltip ? 10 : 0;

            Size size = GetSizeForLabel(lblCharName, bigFontSize, left, top, rightPad,
                labelWidth, labelHeight);
            labelWidth = size.Width;
            labelHeight = size.Height;
            top += labelHeight;

            if (lblBalance.Visible)
            {
                size = GetSizeForLabel(lblBalance, mediumFontSize, left, top, rightPad,
                    labelWidth, labelHeight);
                labelWidth = size.Width;
                labelHeight = size.Height;
                top += labelHeight;
            }
            if (lblTotalSkillPoints.Visible)
            {
                size = GetSizeForLabel(lblTotalSkillPoints, mediumFontSize, left, top,
                    rightPad, labelWidth, labelHeight);
                labelWidth = size.Width;
                labelHeight = size.Height;
                top += labelHeight;
            }
            if (lblRemainingTime.Visible || lblSkillInTraining.Visible || lblCompletionTime.
                    Visible)
                top += verticalMargin;
            if (lblRemainingTime.Visible)
            {
                size = GetSizeForLabel(lblRemainingTime, mediumFontSize, left, top, rightPad,
                    labelWidth, labelHeight);
                labelWidth = size.Width;
                labelHeight = size.Height;
                top += labelHeight;
            }
            if (lblSkillInTraining.Visible)
            {
                size = GetSizeForLabel(lblSkillInTraining, m_regularFontSize, left, top,
                    rightPad, labelWidth, smallLabelHeight);
                labelWidth = size.Width;
                smallLabelHeight = size.Height;
                top += smallLabelHeight;
            }
            if (lblCompletionTime.Visible)
            {
                size = GetSizeForLabel(lblCompletionTime, m_regularFontSize, left, top,
                    rightPad, labelWidth, smallLabelHeight);
                labelWidth = size.Width;
                smallLabelHeight = size.Height;
                top += smallLabelHeight;
            }
            if (lblSkillQueueTrainingTime.Visible)
            {
                size = GetSizeForLabel(lblSkillQueueTrainingTime, m_regularFontSize, left, top,
                    rightPad, labelWidth, smallLabelHeight);
                labelWidth = size.Width;
                smallLabelHeight = size.Height;
                top += smallLabelHeight;
            }
            int lh = portraitSize;
            if (lblExtraInfo.Visible)
            {
                // Below portrait if used, else below last text
                size = GetSizeForLabel(lblExtraInfo, m_regularFontSize, showPortrait ? margin :
                    left, showPortrait ? (margin + portraitSize) : top, rightPad, labelWidth,
                    smallLabelHeight);
                smallLabelHeight = size.Height;
                // Add to correct side of the view
                if (showPortrait)
                {
                    lh += smallLabelHeight;
                    // Avoid overlapping text
                    lblExtraInfo.Width = Math.Min(lblExtraInfo.Width, portraitSize);
                }
                else
                    top += smallLabelHeight;
            }
            Width = m_preferredWidth = left + labelWidth + margin;
            Height = m_preferredHeight = margin + (showPortrait ? Math.Max(top, lh + margin) :
                top);

            ResumeLayout(false);
        }

        /// <summary>
        /// Gets the minimum width.
        /// </summary>
        /// <returns></returns>
        private int GetMinimumWidth(Graphics g)
        {
            if (m_minWidth <= 0)
            {
                // Determine longest skill name
                StaticSkill? longestSkill = null;
                int maxLength = 0;
                foreach (var skill in StaticSkills.AllSkills)
                {
                    int len = skill.Name.Length;
                    if (longestSkill == null || len > maxLength)
                    {
                        maxLength = len;
                        longestSkill = skill;
                    }
                }
                // Use the actual font on the display
                m_minWidth = (int)Math.Ceiling(g.MeasureString(longestSkill!.Name + " " + Skill.
                    GetRomanFromInt(3), lblSkillInTraining.Font).Width);
            }
            return m_minWidth;
        }

        /// <summary>
        /// Gets the size for the specified label.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="left">The left.</param>
        /// <param name="top">The top.</param>
        /// <param name="rightPad">The right pad.</param>
        /// <param name="labelWidth">Width of the label.</param>
        /// <param name="labelHeight">Height of the label.</param>
        /// <returns></returns>
        private static Size GetSizeForLabel(Label label, float fontSize, int left, int top,
            int rightPad, int labelWidth, int labelHeight)
        {
            Font font = FontFactory.GetFont(label.Font.FontFamily, fontSize, label.Font.Style);
            label.Font = font;
            label.Location = new Point(left, top);
            labelWidth = Math.Max(labelWidth, label.PreferredWidth + rightPad);
            labelHeight = Math.Max(labelHeight, font.Height);
            label.Size = new Size(labelWidth, labelHeight);
            return label.Size;
        }

        /// <summary>
        /// Gets the preferred size for control. Used by parents to decide which size they will grant to their children.
        /// </summary>
        /// <param name="proposedSize"></param>
        /// <returns></returns>
        public override Size GetPreferredSize(Size proposedSize) => new Size(m_preferredWidth,
            m_preferredHeight);

        #endregion
    }
}
