using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for Plan Window.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class PlanWindowSettings
    {
        private readonly Collection<PlanColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanWindowSettings"/> class.
        /// </summary>
        public PlanWindowSettings()
        {
            DimUntrainable = true;
            OnlyShowSelectionSummaryOnMultiSelect = true;

            PrioritiesMsgBox = new CustomMsgBoxSettings();
            ObsoleteEntryRemovalBehaviour = ObsoleteEntryRemovalBehaviour.RemoveConfirmed;

            m_columns = new Collection<PlanColumnSettings>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [highlight planned skills].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [highlight planned skills]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("highlightPlannedSkills")]
        public bool HighlightPlannedSkills { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [highlight prerequisites].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [highlight prerequisites]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("highlightPrerequisites")]
        public bool HighlightPrerequisites { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [highlight conflicts].
        /// </summary>
        /// <value><c>true</c> if [highlight conflicts]; otherwise, <c>false</c>.</value>
        [XmlElement("highlightConflicts")]
        public bool HighlightConflicts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [highlight partial skills].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [highlight partial skills]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("highlightPartialSkills")]
        public bool HighlightPartialSkills { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [highlight queued skills].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [highlight queued skills]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("highlightQueuedSkills")]
        public bool HighlightQueuedSkills { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [only show selection summary on multi select].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [only show selection summary on multi select]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("onlyShowSelectionSummaryOnMultiSelect")]
        public bool OnlyShowSelectionSummaryOnMultiSelect { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use advance entry addition].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [use advance entry addition]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("useAdvanceEntryAddition")]
        public bool UseAdvanceEntryAddition { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [dim untrainable].
        /// </summary>
        /// <value><c>true</c> if [dim untrainable]; otherwise, <c>false</c>.</value>
        [XmlElement("dimUntrainable")]
        public bool DimUntrainable { get; set; }

        /// <summary>
        /// Gets or sets the priorities message box settings.
        /// </summary>
        /// <value>The priorities message box settings.</value>
        [XmlElement("prioritiesMsgBox")]
        public CustomMsgBoxSettings PrioritiesMsgBox { get; set; }

        /// <summary>
        /// Gets or sets the obsolete entry removal behaviour.
        /// </summary>
        /// <value>The obsolete entry removal behaviour.</value>
        [XmlElement("obsoleteEntryRemovalBehaviour")]
        public ObsoleteEntryRemovalBehaviour ObsoleteEntryRemovalBehaviour { get; set; }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<PlanColumnSettings> Columns
        {
            get => m_columns;
            set
            {
                m_columns.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                        m_columns.Add(item);
                }
            }
        }

        /// <summary>
        /// Gets the default columns.
        /// </summary>
        /// <value>The default columns.</value>
        public IEnumerable<PlanColumnSettings> DefaultColumns
        {
            get
            {
                PlanColumn[] defaultColumns = new[]
                                                  {
                                                      PlanColumn.Priority,
                                                      PlanColumn.SkillName,
                                                      PlanColumn.TrainingTime,
                                                      PlanColumn.SkillRank,
                                                      PlanColumn.PrimaryAttribute,
                                                      PlanColumn.SecondaryAttribute,
                                                      PlanColumn.SkillGroup,
                                                      PlanColumn.SPPerHour
                                                  };

                return EnumExtensions.GetValues<PlanColumn>().Where(
                    planColumn => planColumn != PlanColumn.None).Where(
                        planColumn => Columns.All(columnSetting => columnSetting.Column != planColumn)).Select(
                            planColumn => new PlanColumnSettings
                                              {
                                                  Column = planColumn,
                                                  Visible = defaultColumns.Contains(planColumn),
                                                  Width = -2
                                              });
            }
        }
    }
}
