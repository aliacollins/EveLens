// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Collections;
using EVEMon.Common.Constants;
using EVEMon.Common.Controls;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Data;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Factories;
using EVEMon.Common.Helpers;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;

using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.ViewModels.Lists;

namespace EVEMon.CharacterMonitoring
{
    internal sealed partial class CharacterWalletTransactionsList : UserControl, IListView
    {
        #region Fields

        private readonly List<WalletTransactionColumnSettings> m_columns = new List<WalletTransactionColumnSettings>();
        private readonly List<WalletTransaction> m_list = new List<WalletTransaction>();

        private WalletTransactionGrouping m_grouping;
        private WalletTransactionColumn m_sortCriteria;

        private string m_textFilter = string.Empty;
        private bool m_sortAscending;
        private bool m_columnsChanged;
        private bool m_isUpdatingColumns;
        private bool m_init;
        private IDisposable? _subConquerableStation;
        private IDisposable? _subWalletTransactions;
        private IDisposable? _subEveIDToName;
        private IDisposable? _tickSub;
        private WalletTransactionsListViewModel? _viewModel;

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public CharacterWalletTransactionsList()
        {
            InitializeComponent();

            lvWalletTransactions.Hide();
            lvWalletTransactions.AllowColumnReorder = true;
            lvWalletTransactions.Columns.Clear();

            noWalletTransactionsLabel.Font = FontFactory.GetFont("Tahoma", 11.25F, FontStyle.Bold);

            ListViewHelper.EnableDoubleBuffer(lvWalletTransactions);

            lvWalletTransactions.ColumnClick += listView_ColumnClick;
            lvWalletTransactions.ColumnWidthChanged += listView_ColumnWidthChanged;
            lvWalletTransactions.ColumnReordered += listView_ColumnReordered;
            lvWalletTransactions.MouseDown += listView_MouseDown;
            lvWalletTransactions.MouseMove += listView_MouseMove;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets the character associated with this monitor.
        /// </summary>
        internal CCPCharacter Character { get; set; } = null!;

        /// <summary>
        /// Gets or sets the text filter.
        /// </summary>
        [Browsable(false)]
        public string TextFilter
        {
            get { return m_textFilter; }
            set
            {
                m_textFilter = value;
                if (m_init)
                    UpdateColumns();
            }
        }

        /// <summary>
        /// Gets or sets the enumeration of wallet transactions to display.
        /// </summary>
        private IEnumerable<WalletTransaction> WalletTransactions
        {
            get { return m_list; }
            set
            {
                m_list.Clear();
                if (value == null)
                    return;

                m_list.AddRange(value);
            }
        }

        /// <summary>
        /// Gets or sets the grouping of a listview.
        /// </summary>
        /// <value></value>
        [Browsable(false)]
        public Enum Grouping
        {
            get { return m_grouping; }
            set
            {
                m_grouping = (WalletTransactionGrouping)value;
                if (m_init)
                    UpdateColumns();
            }
        }

        /// <summary>
        /// Gets or sets the settings used for columns.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public IEnumerable<IColumnSettings> Columns
        {
            get
            {
                // Add the visible columns; matching the display order
                List<WalletTransactionColumnSettings> newColumns = new List<WalletTransactionColumnSettings>();
                foreach (ColumnHeader header in lvWalletTransactions.Columns.Cast<ColumnHeader>().OrderBy(x => x.DisplayIndex))
                {
                    WalletTransactionColumnSettings columnSetting =
                        m_columns.First(x => x.Column == (WalletTransactionColumn)header.Tag!);
                    if (columnSetting.Width > -1)
                        columnSetting.Width = header.Width;

                    newColumns.Add(columnSetting);
                }

                // Then add the other columns
                newColumns.AddRange(m_columns.Where(x => !x.Visible));

                return newColumns;
            }
            set
            {
                m_columns.Clear();
                if (value != null)
                    m_columns.AddRange(value.Cast<WalletTransactionColumnSettings>());

                if (m_init)
                    UpdateColumns();
            }
        }

        #endregion


        # region Inherited Events

        /// <summary>
        /// On load subscribe the events.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode || this.IsDesignModeHosted())
                return;

            _viewModel = new WalletTransactionsListViewModel();

            var agg = AppServices.EventAggregator;
            _tickSub = agg.SubscribeOnUI<EVEMon.Core.Events.FiveSecondTickEvent>(this, e => EveMonClient_TimerTick(null, EventArgs.Empty));
            _subConquerableStation = agg.SubscribeOnUI<ConquerableStationListUpdatedEvent>(this, e => EveMonClient_ConquerableStationListUpdated());
            _subWalletTransactions = agg.SubscribeOnUIForCharacter<CharacterWalletTransactionsUpdatedEvent>(this, () => Character, e => EveMonClient_CharacterWalletTransactionsUpdated(e));
            _subEveIDToName = agg.SubscribeOnUI<EveIDToNameUpdatedEvent>(this, e => EveMonClient_EveIDToNameUpdated());
            Disposed += OnDisposed;
        }

        /// <summary>
        /// Unsubscribe events on disposing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDisposed(object? sender, EventArgs e)
        {
            _viewModel?.Dispose();
            _viewModel = null;

            _tickSub?.Dispose();
            _tickSub = null;
            _subConquerableStation?.Dispose();
            _subWalletTransactions?.Dispose();
            _subEveIDToName?.Dispose();
            Disposed -= OnDisposed;
        }

        /// <summary>
        /// When the control becomes visible again, we update the content.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (DesignMode || this.IsDesignModeHosted() || Character == null || !Visible)
                return;

            // Prevents the properties to call UpdateColumns() till we set all properties
            m_init = false;

            lvWalletTransactions.Visible = false;

            WalletTransactions = Character?.WalletTransactions!;
            Columns = Settings.UI.MainWindow.WalletTransactions.Columns;
            Grouping = Character?.UISettings.WalletTransactionsGroupBy!;
            TextFilter = string.Empty;

            UpdateColumns();

            m_init = true;

            UpdateListVisibility();
        }

        # endregion


        #region Update Methods

        /// <summary>
        /// Autoresizes the columns.
        /// </summary>
        public void AutoResizeColumns()
        {
            m_columns.ForEach(column =>
                                  {
                                      if (column.Visible)
                                          column.Width = -2;
                                  });

            UpdateColumns();
        }

        /// <summary>
        /// Updates the columns.
        /// </summary>
        internal void UpdateColumns()
        {
            // Returns if not visible
            if (!Visible)
                return;

            lvWalletTransactions.BeginUpdate();
            m_isUpdatingColumns = true;

            try
            {
                lvWalletTransactions.Columns.Clear();
                lvWalletTransactions.Groups.Clear();
                lvWalletTransactions.Items.Clear();

                foreach (WalletTransactionColumnSettings column in m_columns.Where(x => x.Visible))
                {
                    ColumnHeader header = lvWalletTransactions.Columns.Add(column.Column.GetHeader(), column.Width);
                    header.Tag = column.Column;

                    switch (column.Column)
                    {
                        case WalletTransactionColumn.Price:
                        case WalletTransactionColumn.Quantity:
                        case WalletTransactionColumn.Credit:
                            header.TextAlign = HorizontalAlignment.Right;
                            break;
                    }
                }

                // We update the content
                UpdateContent();
            }
            finally
            {
                lvWalletTransactions.EndUpdate();
                m_isUpdatingColumns = false;
            }
        }

        /// <summary>
        /// Updates the content of the listview.
        /// </summary>
        private void UpdateContent()
        {
            if (!Visible)
                return;

            int scrollBarPosition = lvWalletTransactions.GetVerticalScrollBarPosition();
            int selectedItem = lvWalletTransactions.SelectedItems.Count > 0
                ? lvWalletTransactions!.SelectedItems[0]!.Tag!.GetHashCode()
                : 0;

            lvWalletTransactions.BeginUpdate();
            try
            {
                // Sync state to VM
                if (_viewModel != null)
                {
                    _viewModel.Character = Character;
                    _viewModel.SortColumn = m_sortCriteria;
                    _viewModel.SortAscending = m_sortAscending;
                    _viewModel.Grouping = m_grouping;
                    _viewModel.TextFilter = m_textFilter;
                }

                var groupedItems = _viewModel?.GroupedItems;

                lvWalletTransactions.Items.Clear();
                lvWalletTransactions.Groups.Clear();

                if (groupedItems != null)
                {
                    bool hasGrouping = groupedItems.Count > 1 ||
                                      (groupedItems.Count == 1 && !string.IsNullOrEmpty(groupedItems[0].Key));

                    foreach (var group in groupedItems)
                    {
                        ListViewGroup? lvGroup = null;
                        if (hasGrouping)
                        {
                            lvGroup = new ListViewGroup(group.Key);
                            lvWalletTransactions.Groups.Add(lvGroup);
                        }

                        foreach (var walletTransaction in group.Items)
                        {
                            var item = new ListViewItem($"{walletTransaction.Date.ToLocalTime()}")
                            {
                                UseItemStyleForSubItems = false,
                                Tag = walletTransaction
                            };
                            if (lvGroup != null) item.Group = lvGroup;
                            CreateSubItems(walletTransaction, item);
                            lvWalletTransactions.Items.Add(item);
                        }
                    }
                }

                UpdateSortVisualFeedback();

                if (selectedItem > 0)
                {
                    foreach (ListViewItem lvItem in lvWalletTransactions.Items.Cast<ListViewItem>().Where(
                        lvItem => lvItem!.Tag!.GetHashCode() == selectedItem))
                    {
                        lvItem.Selected = true;
                    }
                }

                AdjustColumns();
                UpdateListVisibility();
            }
            finally
            {
                lvWalletTransactions.EndUpdate();
                lvWalletTransactions.SetVerticalScrollBarPosition(scrollBarPosition);
            }
        }

        /// <summary>
        /// Updates the list visibility.
        /// </summary>
        private void UpdateListVisibility()
        {
            // Display or hide the "no wallet transactions" label
            if (!m_init)
                return;

            noWalletTransactionsLabel.Visible = lvWalletTransactions.Items.Count == 0;
            lvWalletTransactions.Visible = !noWalletTransactionsLabel.Visible;
        }

        // UpdateContentByGroup REMOVED — grouping is now handled by WalletTransactionsListViewModel.
        // UpdateNoGroupContent REMOVED — grouping is now handled by WalletTransactionsListViewModel.
        // UpdateContent<TKey> REMOVED — grouping is now handled by WalletTransactionsListViewModel.

        /// <summary>
        /// Creates the list view sub items.
        /// </summary>
        /// <param name="walletTransaction">The WalletTransaction.</param>
        /// <param name="item">The item.</param>
        private ListViewItem CreateSubItems(WalletTransaction walletTransaction, ListViewItem item)
        {
            // Add enough subitems to match the number of columns
            while (item.SubItems.Count < lvWalletTransactions.Columns.Count + 1)
            {
                item.SubItems.Add(string.Empty);
            }

            // Creates the subitems
            for (int i = 0; i < lvWalletTransactions.Columns.Count; i++)
            {
                SetColumn(walletTransaction, item.SubItems[i], (WalletTransactionColumn)lvWalletTransactions.Columns[i]!.Tag!);
            }

            return item;
        }

        /// <summary>
        /// Adjusts the columns.
        /// </summary>
        private void AdjustColumns()
        {
            foreach (ColumnHeader column in lvWalletTransactions.Columns)
            {
                if (m_columns[column.Index].Width == -1)
                    m_columns[column.Index].Width = -2;

                column.Width = m_columns[column.Index].Width;

                // Due to .NET design we need to prevent the last colummn to resize to the right end

                // Return if it's not the last column and not set to auto-resize
                if (column.Index != lvWalletTransactions.Columns.Count - 1 || m_columns[column.Index].Width != -2)
                    continue;

                const int Pad = 4;

                // Calculate column header text width with padding
                int columnHeaderWidth = TextRenderer.MeasureText(column.Text, Font).Width + Pad * 2;

                // If there is an image assigned to the header, add its width with padding
                if (lvWalletTransactions.SmallImageList != null && column.ImageIndex > -1)
                    columnHeaderWidth += lvWalletTransactions.SmallImageList.ImageSize.Width + Pad;

                // Calculate the width of the header and the items of the column
                int columnMaxWidth = column!.ListView!.Items.Cast<ListViewItem>().Select(
                    item => TextRenderer.MeasureText(item.SubItems[column.Index].Text, Font).Width).Concat(
                        new[] { columnHeaderWidth }).Max() + Pad + 1;

                // Assign the width found
                column.Width = columnMaxWidth;
            }
        }

        // UpdateSort REMOVED — sorting is now handled by WalletTransactionsListViewModel.

        /// <summary>
        /// Updates the sort feedback (the arrow on the header).
        /// </summary>
        private void UpdateSortVisualFeedback()
        {
            foreach (ColumnHeader columnHeader in lvWalletTransactions.Columns.Cast<ColumnHeader>())
            {
                WalletTransactionColumn column = (WalletTransactionColumn)columnHeader.Tag!;
                if (m_sortCriteria == column)
                    columnHeader.ImageIndex = m_sortAscending ? 0 : 1;
                else
                    columnHeader.ImageIndex = 2;
            }
        }

        /// <summary>
        /// Formats the price according to the settings.
        /// </summary>
        /// <param name="price">The price to display.</param>
        /// <returns>The price as a string.</returns>
        private static string FormatPrice(decimal price)
        {
            return FormatHelper.FormatIf(Settings.UI.MainWindow.WalletTransactions.
                NumberAbsFormat, 2, price, AbbreviationFormat.AbbreviationSymbols);
        }

        /// <summary>
        /// Updates the listview sub-item.
        /// </summary>
        /// <param name="walletTransaction"></param>
        /// <param name="item"></param>
        /// <param name="column"></param>
        private static void SetColumn(WalletTransaction walletTransaction, ListViewItem.ListViewSubItem item,
                                      WalletTransactionColumn column)
        {
            switch (column)
            {
            case WalletTransactionColumn.Date:
                item.Text = walletTransaction.Date.ToLocalTime().ToString("G");
                break;
            case WalletTransactionColumn.ItemName:
                item.Text = walletTransaction.ItemName;
                break;
            case WalletTransactionColumn.Price:
                item.Text = FormatPrice(walletTransaction.Price);
                break;
            case WalletTransactionColumn.Quantity:
                item.Text = FormatHelper.FormatIf(Settings.UI.MainWindow.WalletTransactions.
                    NumberAbsFormat, walletTransaction.Quantity, AbbreviationFormat.
                    AbbreviationSymbols);
                break;
            case WalletTransactionColumn.Credit:
                item.Text = FormatPrice(walletTransaction.Credit);
                item.ForeColor = walletTransaction.TransactionType == TransactionType.Buy ?
                    Color.DarkRed : Color.DarkGreen;
                break;
            case WalletTransactionColumn.Client:
                item.Text = walletTransaction.ClientName;
                break;
            case WalletTransactionColumn.Location:
                item.Text = walletTransaction.Station.FullLocation;
                break;
            case WalletTransactionColumn.Region:
                item.Text = walletTransaction.Station.SolarSystemChecked.Constellation.
                    Region.Name;
                break;
            case WalletTransactionColumn.SolarSystem:
                item.Text = walletTransaction.Station.SolarSystem?.Name ??
                    EveMonConstants.UnknownText;
                item.ForeColor = walletTransaction.Station.SolarSystemChecked.
                    SecurityLevelColor;
                break;
            case WalletTransactionColumn.Station:
                item.Text = walletTransaction.Station.Name;
                break;
            case WalletTransactionColumn.TransactionFor:
                item.Text = walletTransaction.TransactionFor.ToString();
                break;
            case WalletTransactionColumn.JournalID:
                item.Text = walletTransaction.JournalID.ToString(CultureConstants.
                    DefaultCulture);
                break;
            default:
                throw new NotImplementedException();
            }
        }

        #endregion


        #region Helper Methods

        // IsTextMatching REMOVED — text filtering is now handled by WalletTransactionsListViewModel.

        #endregion


        #region Local Event Handlers

        /// <summary>
        /// Exports item info to CSV format.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exportToCSVToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            ListViewExporter.CreateCSV(lvWalletTransactions);
        }

        /// <summary>
        /// On column reorder we update the settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_ColumnReordered(object? sender, ColumnReorderedEventArgs e)
        {
            m_columnsChanged = true;
        }

        /// <summary>
        /// When the user manually resizes a column, we make sure to update the column preferences.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_ColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        {
            if (m_isUpdatingColumns || m_columns.Count <= e.ColumnIndex)
                return;

            if (m_columns[e.ColumnIndex].Width == lvWalletTransactions.Columns[e.ColumnIndex].Width)
                return;

            m_columns[e.ColumnIndex].Width = lvWalletTransactions.Columns[e.ColumnIndex].Width;
            m_columnsChanged = true;
        }

        /// <summary>
        /// When the user clicks a column header, we update the sorting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            WalletTransactionColumn column = (WalletTransactionColumn)lvWalletTransactions.Columns![e.Column].Tag!;
            if (m_sortCriteria == column)
                m_sortAscending = !m_sortAscending;
            else
            {
                m_sortCriteria = column;
                m_sortAscending = true;
            }

            m_isUpdatingColumns = true;

            if (_viewModel != null)
            {
                _viewModel.SortColumn = m_sortCriteria;
                _viewModel.SortAscending = m_sortAscending;
            }
            UpdateSortVisualFeedback();

            m_isUpdatingColumns = false;
        }

        /// <summary>
        /// When the mouse gets pressed, we change the cursor.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void listView_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            lvWalletTransactions.Cursor = Cursors.Default;
        }

        /// <summary>
        /// When the mouse moves over the list, we change the cursor.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void listView_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                return;

            lvWalletTransactions.Cursor = CustomCursors.ContextMenu;
        }

        # endregion


        #region Global Events

        /// <summary>
        /// On timer tick, we update the column settings if any changes have been made to them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EveMonClient_TimerTick(object? sender, EventArgs e)
        {
            if (!Visible || !m_columnsChanged)
                return;

            Settings.UI.MainWindow.WalletTransactions.Columns.Clear();
            Settings.UI.MainWindow.WalletTransactions.Columns.AddRange(Columns.Cast<WalletTransactionColumnSettings>());

            // Recreate the columns
            Columns = Settings.UI.MainWindow.WalletTransactions.Columns;
            m_columnsChanged = false;
        }

        /// <summary>
        /// When the wallet transactions change update the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EveMonClient_CharacterWalletTransactionsUpdated(CharacterWalletTransactionsUpdatedEvent e)
        {
            WalletTransactions = Character.WalletTransactions;
            UpdateColumns();
        }

        /// <summary>
        /// When Conquerable Station List updates, update the list.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_ConquerableStationListUpdated()
        {
            foreach (WalletTransaction walletTransaction in m_list)
            {
                walletTransaction.UpdateStation();
            }

            UpdateColumns();
        }

        /// <summary>
        /// When EVE ID to name updates, update the list.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveMonClient_EveIDToNameUpdated()
        {
            UpdateColumns();
        }

        #endregion
    }
}
