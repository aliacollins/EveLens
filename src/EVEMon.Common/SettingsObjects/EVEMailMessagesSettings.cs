// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for EVE Mail Messages.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class EveMailMessagesSettings
    {
        private readonly Collection<EveMailMessagesColumnSettings> m_columns;

        /// <summary>
        /// Initializes a new instance of the <see cref="EveMailMessagesSettings"/> class.
        /// </summary>
        public EveMailMessagesSettings()
        {
            m_columns = new Collection<EveMailMessagesColumnSettings>();

            ReadingPanePosition = ReadingPanePositioning.Off;
        }

        /// <summary>
        /// Gets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [XmlArray("columns")]
        [XmlArrayItem("column")]
        public Collection<EveMailMessagesColumnSettings> Columns
        {
            get { return m_columns; }
        }

        /// <summary>
        /// Gets or sets the reading pane position.
        /// </summary>
        /// <value>The reading pane position.</value>
        [XmlElement("readingPanePosition")]
        public ReadingPanePositioning ReadingPanePosition { get; set; }

        /// <summary>
        /// Gets the default columns.
        /// </summary>
        /// <value>The default columns.</value>
        public IEnumerable<EveMailMessagesColumnSettings> DefaultColumns
        {
            get
            {
                EveMailMessagesColumn[] defaultColumns = new[]
                                                             {
                                                                 EveMailMessagesColumn.SenderName,
                                                                 EveMailMessagesColumn.Title,
                                                                 EveMailMessagesColumn.SentDate,
                                                                 EveMailMessagesColumn.ToCharacters
                                                             };

                return EnumExtensions.GetValues<EveMailMessagesColumn>().Where(
                    planColumn => planColumn != EveMailMessagesColumn.None).Where(
                        planColumn => Columns.All(columnSetting => columnSetting.Column != planColumn)).Select(
                            planColumn => new EveMailMessagesColumnSettings
                                              {
                                                  Column = planColumn,
                                                  Visible = defaultColumns.Contains(planColumn),
                                                  Width = -2
                                              });
            }
        }
    }
}