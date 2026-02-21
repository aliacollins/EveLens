// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using EVEMon.Common.Models.Comparers;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.ViewModels.Lists;
using EVEMon.SkillPlanner;
using Region = EVEMon.Common.Data.Region;

namespace EVEMon.CharacterMonitoring
{
    internal sealed partial class CharacterAssetsList : UserControl, IListView
    {
        #region Fields

        private readonly List<AssetColumnSettings> m_columns = new List<AssetColumnSettings>();
        private readonly List<Asset> m_list = new List<Asset>();

        private InfiniteDisplayToolTip m_tooltip = null!;
        private AssetGrouping m_grouping;
        private AssetColumn m_sortCriteria;

        private string m_textFilter = string.Empty;
        private string m_totalCostLabelDefaultText = null!;

        private bool m_sortAscending = true;
        private bool m_columnsChanged;
        private bool m_isUpdatingColumns;
        private bool m_init;

        // Virtual mode support for large lists
        private List<Asset> m_virtualModeItems = null!;
        private bool m_isVirtualMode;

        /// <summary>
        /// Threshold for enabling virtual mode. Lists larger than this will use virtual mode
        /// when not using groups (virtual mode doesn't support ListView groups).
        /// </summary>
        private const int VirtualModeThreshold = 500;
        private IDisposable? _subAssets;
        private IDisposable? _subCharInfo;
        private IDisposable? _subConquerableStation;
        private IDisposable? _subEveFlags;
        private IDisposable? _subSettings;
        private IDisposable? _subItemPrices;
        private IDisposable? _tickSub;

        /// <summary>
        /// ViewModel for this list (Strangler Fig: coexists with existing code,
        /// will gradually assume filter/sort/group responsibilities).
        /// </summary>
        private AssetsListViewModel? _viewModel;

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public CharacterAssetsList()
        {
            InitializeComponent();

            lvAssets.Visible = false;
            lvAssets.AllowColumnReorder = true;
            lvAssets.Columns.Clear();
            estimatedCostPanel.Hide();
            noPricesFoundLabel.Hide();

            noAssetsLabel.Font = FontFactory.GetFont("Segoe UI", 11.25F, FontStyle.Bold);

            ListViewHelper.EnableDoubleBuffer(lvAssets);

            lvAssets.KeyDown += listView_KeyDown;
            lvAssets.ColumnClick += listView_ColumnClick;
            lvAssets.ColumnWidthChanged += listView_ColumnWidthChanged;
            lvAssets.ColumnReordered += listView_ColumnReordered;
            lvAssets.MouseDown += listView_MouseDown;
            lvAssets.MouseMove += listView_MouseMove;
            lvAssets.MouseLeave += listView_MouseLeave;
            lvAssets.RetrieveVirtualItem += listView_RetrieveVirtualItem;
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
                    Task.WhenAll(UpdateColumnsAsync());
            }
        }

        /// <summary>
        /// Gets or sets the enumeration of assets to display.
        /// </summary>
        private IEnumerable<Asset> Assets
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
                m_grouping = (AssetGrouping)value;
                if (m_init)
                    Task.WhenAll(UpdateColumnsAsync());
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
                List<AssetColumnSettings> newColumns = new List<AssetColumnSettings>();
                foreach (ColumnHeader header in lvAssets.Columns.Cast<ColumnHeader>().OrderBy(x => x.DisplayIndex))
                {
                    AssetColumnSettings columnSetting = m_columns.First(x => x.Column == (AssetColumn)header.Tag!);
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
                    m_columns.AddRange(value.Cast<AssetColumnSettings>());

                if (m_init)
                    Task.WhenAll(UpdateColumnsAsync());
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

            m_totalCostLabelDefaultText = lblTotalCost.Text;

            m_tooltip = new InfiniteDisplayToolTip(lvAssets);

            _viewModel = new AssetsListViewModel();

            var agg = AppServices.EventAggregator;
            _tickSub = agg.SubscribeOnUI<EVEMon.Core.Events.FiveSecondTickEvent>(this, e => EveMonClient_TimerTick(null, EventArgs.Empty));
            _subAssets = agg.SubscribeOnUIForCharacter<CharacterAssetsUpdatedEvent>(this, () => Character, e => EveMonClient_CharacterAssetsUpdated(e));
            _subCharInfo = agg.SubscribeOnUIForCharacter<CharacterInfoUpdatedEvent>(this, () => Character, e => EveMonClient_CharacterInfoUpdated(e));
            _subConquerableStation = agg.SubscribeOnUI<ConquerableStationListUpdatedEvent>(this, e => EveMonClient_ConquerableStationListUpdated());
            _subEveFlags = agg.SubscribeOnUI<EveFlagsUpdatedEvent>(this, e => EveMonClient_EveFlagsUpdated());
            _subSettings = agg.SubscribeOnUI<SettingsChangedEvent>(this, e => EveMonClient_SettingsChanged());
            _subItemPrices = agg.SubscribeOnUI<ItemPricesUpdatedEvent>(this, e => EveMonClient_ItemPricesUpdated());
            Disposed += OnDisposed;
        }

        /// <summary>
        /// Unsubscribe events on disposing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDisposed(object? sender, EventArgs e)
        {
            m_tooltip.Dispose();

            _viewModel?.Dispose();
            _viewModel = null;

            _tickSub?.Dispose();
            _tickSub = null;
            _subAssets?.Dispose();
            _subCharInfo?.Dispose();
            _subConquerableStation?.Dispose();
            _subEveFlags?.Dispose();
            _subSettings?.Dispose();
            _subItemPrices?.Dispose();
            Disposed -= OnDisposed;
        }

        /// <summary>
        /// When the control becomes visible again, we update the content.
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnVisibleChanged(EventArgs e)
        {
            try
            {
                if (DesignMode || this.IsDesignModeHosted() || Character == null)
                    return;

                base.OnVisibleChanged(e);

                if (!Visible)
                    return;

                // Prevents the properties to call UpdateColumnsAsync() till we set all properties
                m_init = false;

                lvAssets.Hide();
                estimatedCostPanel.Hide();
                noAssetsLabel.Visible = Character?.Assets.Count == 0;

                if (_viewModel != null)
                    _viewModel.Character = Character;

                Assets = Character?.Assets!;
                Columns = Settings.UI.MainWindow.Assets.Columns;
                Grouping = Character?.UISettings.AssetsGroupBy!;
                TextFilter = string.Empty;

                await UpdateColumnsAsync();

                m_init = true;

                UpdateListVisibility();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
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

            UpdateColumnsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the columns.
        /// </summary>
        internal async Task UpdateColumnsAsync()
        {
            // Returns if not visible
            if (!Visible)
                return;

            lvAssets.BeginUpdate();
            m_isUpdatingColumns = true;

            lvAssets.Hide();
            noAssetsLabel.Hide();

            lvAssets.Columns.Clear();
            lvAssets.Groups.Clear();
            lvAssets.Items.Clear();

            try
            {
                throbber.Show();
                throbber.State = ThrobberState.Rotating;
                
                AddColumns();

                // We update the content
                await UpdateContentAsync();

                throbber.State = ThrobberState.Stopped;
                throbber.Hide();
            }
            finally
            {
                lvAssets.EndUpdate();
                m_isUpdatingColumns = false;
            }
        }

        /// <summary>
        /// Adds the columns.
        /// </summary>
        private void AddColumns()
        {
            foreach (AssetColumnSettings column in m_columns.Where(x => x.Visible))
            {
                ColumnHeader header = lvAssets.Columns.Add(column.Column.GetHeader(), column.Width);
                header.Tag = column.Column;

                switch (column.Column)
                {
                    case AssetColumn.UnitaryPrice:
                    case AssetColumn.TotalPrice:
                    case AssetColumn.Quantity:
                    case AssetColumn.Volume:
                        header.TextAlign = HorizontalAlignment.Right;
                        break;
                }
            }
        }

        /// <summary>
        /// Updates the content of the listview using the ViewModel's filter/sort/group pipeline.
        /// </summary>
        private async Task UpdateContentAsync()
        {
            // Returns if not visible
            if (!Visible)
                return;

            int scrollBarPosition = lvAssets.GetVerticalScrollBarPosition();

            // Store the selected item (if any) to restore it after the update
            Asset? firstSelected = GetFirstSelectedAsset();
            int selectedItem = firstSelected?.GetHashCode() ?? 0;

            lvAssets.BeginUpdate();
            try
            {
                // Sync state to VM and refresh the data pipeline
                if (_viewModel != null)
                {
                    _viewModel.Character = Character;
                    _viewModel.SortColumn = m_sortCriteria;
                    _viewModel.SortAscending = m_sortAscending;
                    _viewModel.Grouping = m_grouping;
                    _viewModel.TextFilter = m_textFilter;
                }

                // Read filtered/sorted/grouped data from VM
                var groupedItems = _viewModel?.GroupedItems;
                var allItems = new List<Asset>();

                if (groupedItems != null && groupedItems.Count > 0)
                {
                    bool hasGrouping = groupedItems.Count > 1 ||
                                       !string.IsNullOrEmpty(groupedItems[0].Key);
                    bool useVirtualMode = !hasGrouping &&
                                          groupedItems[0].Items.Count > VirtualModeThreshold;

                    if (useVirtualMode)
                    {
                        // Virtual mode for large ungrouped lists
                        var items = groupedItems[0].Items.ToList();
                        if (!m_isVirtualMode)
                        {
                            m_isVirtualMode = true;
                            lvAssets.VirtualMode = true;
                            lvAssets.ListViewItemSorter = null;
                        }
                        m_virtualModeItems = items;
                        lvAssets.Groups.Clear();
                        lvAssets.VirtualListSize = items.Count;
                        allItems.AddRange(items);
                    }
                    else
                    {
                        // Normal mode: disable virtual mode if active
                        if (m_isVirtualMode)
                        {
                            m_isVirtualMode = false;
                            lvAssets.VirtualMode = false;
                            m_virtualModeItems = null!;
                        }

                        lvAssets.Items.Clear();
                        lvAssets.Groups.Clear();

                        foreach (var group in groupedItems)
                        {
                            ListViewGroup? lvGroup = null;
                            if (hasGrouping)
                            {
                                lvGroup = new ListViewGroup(group.Key);
                                lvAssets.Groups.Add(lvGroup);
                            }

                            foreach (var asset in group.Items)
                            {
                                var item = new ListViewItem(asset.Item.Name)
                                {
                                    UseItemStyleForSubItems = false,
                                    Tag = asset
                                };
                                if (lvGroup != null)
                                    item.Group = lvGroup;
                                CreateSubItems(asset, item);
                                lvAssets.Items.Add(item);
                                allItems.Add(asset);
                            }
                        }
                    }
                }
                else
                {
                    // No items
                    if (m_isVirtualMode)
                    {
                        m_isVirtualMode = false;
                        lvAssets.VirtualMode = false;
                        m_virtualModeItems = null!;
                    }
                    lvAssets.Items.Clear();
                    lvAssets.Groups.Clear();
                }

                UpdateSortVisualFeedback();

                await UpdateItemsCostAsync(allItems);

                // Adjust the size of the columns
                AdjustColumns();

                UpdateListVisibility();

                // Restore the selected item (if any)
                if (selectedItem > 0)
                {
                    if (m_isVirtualMode)
                    {
                        for (int i = 0; i < m_virtualModeItems.Count; i++)
                        {
                            if (m_virtualModeItems[i].GetHashCode() == selectedItem)
                            {
                                lvAssets.SelectedIndices.Add(i);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (ListViewItem lvItem in lvAssets.Items.Cast<ListViewItem>().Where(
                            lvItem => lvItem!.Tag!.GetHashCode() == selectedItem))
                        {
                            lvItem.Selected = true;
                        }
                    }
                }
            }
            finally
            {
                lvAssets.EndUpdate();
                lvAssets.SetVerticalScrollBarPosition(scrollBarPosition);
            }
        }

        /// <summary>
        /// Updates the items cost.
        /// </summary>
        /// <param name="assets">The assets.</param>
        private async Task UpdateItemsCostAsync(IList<Asset> assets)
        {
            lblTotalCost.Text = string.Format(CultureConstants.DefaultCulture,
                m_totalCostLabelDefaultText, await TaskHelper.RunCPUBoundTaskAsync(() =>
                assets.Sum(asset => asset.Price * asset.Quantity)));

            if (!totalCostThrobber.Visible && !Settings.MarketPricer.Pricer.Queried)
            {
                noPricesFoundLabel.Hide();
                totalCostThrobber.Show();
                totalCostThrobber.State = ThrobberState.Rotating;
                return;
            }

            totalCostThrobber.State = ThrobberState.Stopped;
            totalCostThrobber.Hide();
            noPricesFoundLabel.Visible = await TaskHelper.RunCPUBoundTaskAsync(() =>
                assets.Where(asset => asset.TypeOfBlueprint != BlueprintType.Copy.ToString()).
                Any(asset => Math.Abs(asset.Price) < double.Epsilon));
        }

        /// <summary>
        /// Updates the list visibility.
        /// </summary>
        private void UpdateListVisibility()
        {
            // Display or hide the "no assets" label
            if (!m_init)
                return;

            noAssetsLabel.Visible = lvAssets.Items.Count == 0;
            estimatedCostPanel.Visible = !noAssetsLabel.Visible;
            lvAssets.Visible = !noAssetsLabel.Visible;
        }

        // UpdateContentByGroupAsync, UpdateNoGroupContentAsync, UpdateContentAsync<TKey>
        // REMOVED — filter/sort/group pipeline is now handled by AssetsListViewModel.
        // UpdateContentAsync reads from _viewModel.GroupedItems directly.

        /// <summary>
        /// Creates the list view sub items.
        /// </summary>
        /// <param name="asset">The asset.</param>
        /// <param name="item">The item.</param>
        private ListViewItem CreateSubItems(Asset asset, ListViewItem item)
        {
            // Add enough subitems to match the number of columns
            while (item.SubItems.Count < lvAssets.Columns.Count + 1)
            {
                item.SubItems.Add(string.Empty);
            }

            // Creates the subitems
            for (int i = 0; i < lvAssets.Columns.Count; i++)
            {
                SetColumn(asset, item.SubItems[i], (AssetColumn)lvAssets.Columns[i]!.Tag!);
            }

            return item;
        }

        /// <summary>
        /// Adjusts the columns.
        /// </summary>
        private void AdjustColumns()
        {
            foreach (ColumnHeader column in lvAssets.Columns)
            {
                if (m_columns[column.Index].Width == -1)
                    m_columns[column.Index].Width = -2;

                column.Width = m_columns[column.Index].Width;

                // Due to .NET design we need to prevent the last colummn to resize to the right end

                // Return if it's not the last column and not set to auto-resize
                if (column.Index != lvAssets.Columns.Count - 1 || m_columns[column.Index].Width != -2)
                    continue;

                const int Pad = 4;

                // Calculate column header text width with padding
                int columnHeaderWidth = TextRenderer.MeasureText(column.Text, Font).Width + Pad * 2;

                // If there is an image assigned to the header, add its width with padding
                if (lvAssets.SmallImageList != null && column.ImageIndex > -1)
                    columnHeaderWidth += lvAssets.SmallImageList.ImageSize.Width + Pad;

                // Calculate the width of the header and the items of the column
                int columnMaxWidth;
                if (m_isVirtualMode)
                {
                    // In virtual mode, we cannot enumerate ListView.Items.
                    // Use header width only — the column was already set to auto-resize (-2).
                    columnMaxWidth = columnHeaderWidth + Pad + 1;
                }
                else
                {
                    columnMaxWidth = column!.ListView!.Items.Cast<ListViewItem>().Select(
                        item => TextRenderer.MeasureText(item.SubItems[column.Index].Text, Font).Width).Concat(
                            new[] { columnHeaderWidth }).Max() + Pad + 1;
                }

                // Assign the width found
                column.Width = columnMaxWidth;
            }
        }

        // UpdateSort REMOVED — sorting is now handled by AssetsListViewModel.

        /// <summary>
        /// Updates the sort feedback (the arrow on the header).
        /// </summary>
        private void UpdateSortVisualFeedback()
        {
            foreach (ColumnHeader columnHeader in lvAssets.Columns.Cast<ColumnHeader>())
            {
                AssetColumn column = (AssetColumn)columnHeader.Tag!;
                if (m_sortCriteria == column)
                    columnHeader.ImageIndex = m_sortAscending ? 0 : 1;
                else
                    columnHeader.ImageIndex = 2;
            }
        }

        /// <summary>
        /// Updates the listview sub-item.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="item"></param>
        /// <param name="column"></param>
        private static void SetColumn(Asset asset, ListViewItem.ListViewSubItem item, AssetColumn column)
        {
            bool numberFormat = Settings.UI.MainWindow.Assets.NumberAbsFormat;

            switch (column)
            {
                case AssetColumn.ItemName:
                    item.Text = asset.Item.Name;
                    break;
                case AssetColumn.Quantity:
                    item.Text = numberFormat
                        ? FormatHelper.Format(asset.Quantity, AbbreviationFormat.AbbreviationSymbols)
                        : asset.Quantity.ToNumericString(0);
                    break;
                case AssetColumn.UnitaryPrice:
                    item.Text = numberFormat
                        ? FormatHelper.Format(asset.Price, AbbreviationFormat.AbbreviationSymbols)
                        : asset.Price.ToNumericString(2);
                    break;
                case AssetColumn.TotalPrice:
                    item.Text = numberFormat
                        ? FormatHelper.Format(asset.Cost, AbbreviationFormat.AbbreviationSymbols)
                        : asset.Cost.ToNumericString(2);
                    break;
                case AssetColumn.Volume:
                    item.Text = numberFormat
                        ? FormatHelper.Format(asset.TotalVolume, AbbreviationFormat.AbbreviationSymbols)
                        : asset.TotalVolume.ToNumericString(2);
                    break;
                case AssetColumn.BlueprintType:
                    item.Text = asset.TypeOfBlueprint;
                    break;
                case AssetColumn.Group:
                    item.Text = asset.Item.GroupName;
                    break;
                case AssetColumn.Category:
                    item.Text = asset.Item.CategoryName;
                    break;
                case AssetColumn.Container:
                    item.Text = asset.Container;
                    break;
                case AssetColumn.Flag:
                    item.Text = asset.Flag;
                    break;
                case AssetColumn.Location:
                    item.Text = asset.Location;
                    item.ForeColor = asset.SolarSystem.SecurityLevelColor;
                    break;
                case AssetColumn.Region:
                    item.Text = asset.SolarSystem.Constellation.Region.Name;
                    break;
                case AssetColumn.SolarSystem:
                    item.Text = asset.SolarSystem.Name;
                    item.ForeColor = asset.SolarSystem.SecurityLevelColor;
                    break;
                case AssetColumn.FullLocation:
                    item.Text = asset.FullLocation;
                    break;
                case AssetColumn.Jumps:
                    item.Text = asset.JumpsText;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Gets the number of selected items, compatible with virtual mode.
        /// </summary>
        private int SelectedItemCount
            => m_isVirtualMode ? lvAssets.SelectedIndices.Count : lvAssets.SelectedItems.Count;

        /// <summary>
        /// Gets the asset for the first selected item, compatible with virtual mode.
        /// Returns null if nothing is selected.
        /// </summary>
        private Asset? GetFirstSelectedAsset()
        {
            if (m_isVirtualMode)
            {
                if (lvAssets.SelectedIndices.Count == 0)
                    return null;

                int index = lvAssets.SelectedIndices[0];
                return index >= 0 && index < m_virtualModeItems.Count
                    ? m_virtualModeItems[index]
                    : null;
            }

            return lvAssets.SelectedItems.Count > 0
                ? lvAssets.SelectedItems[0]?.Tag as Asset
                : null;
        }

        /// <summary>
        /// Gets all selected assets, compatible with virtual mode.
        /// </summary>
        private List<Asset> GetSelectedAssets()
        {
            if (m_isVirtualMode)
            {
                var assets = new List<Asset>();
                foreach (int index in lvAssets.SelectedIndices.Cast<int>())
                {
                    if (index >= 0 && index < m_virtualModeItems.Count)
                        assets.Add(m_virtualModeItems[index]);
                }
                return assets;
            }

            return lvAssets.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => item.Tag)
                .OfType<Asset>()
                .ToList();
        }

        // IsTextMatching REMOVED — text filtering is now handled by AssetsListViewModel.MatchesFilter.

        /// <summary>
        /// Gets the tool tip text.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private string GetToolTipText(ListViewItem item)
        {
            if (!item.Selected || SelectedItemCount < 2)
                return string.Empty;

            List<Asset> selectedAssets = GetSelectedAssets();
            if (selectedAssets.Any(a => a.Item?.Name != item.Text))
                return string.Empty;
            long sumQuantity = selectedAssets.Sum(selectedAsset => selectedAsset.Quantity);
            double sumVolume = selectedAssets.Sum(selectedAsset => selectedAsset.TotalVolume);
            int uniqueLocations = selectedAssets.Select(asset => asset.Location).Distinct().Count();
            int minJumps = selectedAssets.Min(asset => asset.Jumps);
            int maxJumps = selectedAssets.Max(asset => asset.Jumps);
            Asset closestAsset = selectedAssets.First(asset => asset.Jumps == minJumps);
            Asset farthestAsset = selectedAssets.Last(asset => asset.Jumps == maxJumps);

            StringBuilder builder = new StringBuilder();
            builder.Append($"{item.Text} ({selectedAssets.First().Volume:N2} m³)")
                .AppendLine()
                .Append($"Total Quantity: {sumQuantity:N0} in {uniqueLocations:N0} " +
                        $"{(uniqueLocations > 1 ? "different " : string.Empty)}location{uniqueLocations.S()}")
                .AppendLine()
                .Append($"Total Volume: {sumVolume:N2} m³")
                .AppendLine()
                .Append($"Closest Location: {closestAsset.Location} ({closestAsset.JumpsText})")
                .AppendLine();

            if (closestAsset.Location != farthestAsset.Location)
                builder.Append($"Farthest Location: {farthestAsset.Location} ({farthestAsset.JumpsText})");

            return builder.ToString();
        }

        /// <summary>
        /// Updates the asset location.
        /// </summary>
        private async Task UpdateAssetLocationAsync()
        {
            // Invoke it on a worker thread cause it may be time intensive
            // if character owns many stuff in several locations
            await TaskHelper.RunCPUBoundTaskAsync(() =>
            {
                Character.Assets.UpdateLocation();
                lock (m_list)
                {
                    Assets = Character.Assets;
                }
            });

            await UpdateColumnsAsync();
        }

        #endregion


        #region Local Event Handlers

        /// <summary>
        /// Exports item info to CSV format.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exportToCSVToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            ListViewExporter.CreateCSV(lvAssets);
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

            if (m_columns[e.ColumnIndex].Width == lvAssets.Columns[e.ColumnIndex].Width)
                return;

            m_columns[e.ColumnIndex].Width = lvAssets.Columns[e.ColumnIndex].Width;
            m_columnsChanged = true;
        }

        /// <summary>
        /// When the user clicks a column header, we update the sorting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            AssetColumn column = (AssetColumn)lvAssets.Columns![e.Column].Tag!;
            if (m_sortCriteria == column)
                m_sortAscending = !m_sortAscending;
            else
            {
                m_sortCriteria = column;
                m_sortAscending = true;
            }

            m_isUpdatingColumns = true;

            // Delegate sort to VM and repopulate
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

            lvAssets.Cursor = Cursors.Default;
        }

        /// <summary>
        /// When the mouse moves over the list, we show the item's tooltip if over an item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void listView_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                return;

            lvAssets.Cursor = CustomCursors.ContextMenu;

            ListViewItem? item = lvAssets.GetItemAt(e.Location.X, e.Location.Y);
            if (item == null)
            {
                m_tooltip.Hide();
                return;
            }

            m_tooltip.Show(GetToolTipText(item), e.Location);
        }

        /// <summary>
        /// When the mouse leaves the list, we hide the item's tooltip.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void listView_MouseLeave(object? sender, EventArgs e)
        {
            m_tooltip.Hide();
        }

        /// <summary>
        /// Handles key press.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.A:
                    if (e.Control)
                        lvAssets.SelectAll();
                    break;
            }
        }

        /// <summary>
        /// Handles the RetrieveVirtualItem event for virtual mode.
        /// Creates list view items on demand for better performance with large lists.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments containing the item index.</param>
        private void listView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (!m_isVirtualMode || m_virtualModeItems == null || e.ItemIndex >= m_virtualModeItems.Count)
            {
                // Create a placeholder item if something goes wrong
                e.Item = new ListViewItem("Loading...");
                return;
            }

            var asset = m_virtualModeItems[e.ItemIndex];
            var item = new ListViewItem(asset.Item?.Name ?? "Unknown")
            {
                UseItemStyleForSubItems = false,
                Tag = asset
            };

            // Create the subitems
            CreateSubItems(asset, item);
            e.Item = item;
        }

        /// <summary>
        /// Handles the Opening event of the contextMenu control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs"/> instance containing the event data.</param>
        private void contextMenu_Opening(object? sender, CancelEventArgs e)
        {
            Asset? firstAsset = GetFirstSelectedAsset();
            bool visible = SelectedItemCount > 0 && firstAsset?.Item != null;

            if (visible)
            {
                string firstName = firstAsset!.Item.Name;
                visible = GetSelectedAssets().All(a => a.Item?.Name == firstName);
            }

            showInBrowserMenuItem.Visible =
                showInBrowserMenuSeparator.Visible = visible;

            if (!visible)
                return;

            Asset? asset = firstAsset;

            if (asset?.Item == null)
                return;

            Blueprint blueprint = StaticBlueprints.GetBlueprintByID(asset.Item.ID);
            Ship? ship = asset.Item as Ship;
            Skill skill = Character.Skills[asset.Item.ID];

            if (skill == Skill.UnknownSkill)
                skill = null!;

            string text = ship != null ? "Ship" : blueprint != null ? "Blueprint" : skill != null ? "Skill" : "Item";

            showInBrowserMenuItem.Text = $"Show In {text} Browser...";
        }

        /// <summary>
        /// Handles the Click event of the showInBrowserMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void showInBrowserMenuItem_Click(object? sender, EventArgs e)
        {
            Asset? asset = GetFirstSelectedAsset();

            if (asset?.Item == null)
                return;

            Ship? ship = asset.Item as Ship;
            Blueprint blueprint = StaticBlueprints.GetBlueprintByID(asset.Item.ID);
            Skill skill = Character.Skills[asset.Item.ID];

            if (skill == Skill.UnknownSkill)
                skill = null!;

            PlanWindow? planWindow = PlanWindow.ShowPlanWindow(Character);

            if (ship != null)
                planWindow!.ShowShipInBrowser(ship);
            else if (blueprint != null)
                planWindow!.ShowBlueprintInBrowser(blueprint);
            else if (skill != null)
                planWindow!.ShowSkillInBrowser(skill);
            else
                planWindow!.ShowItemInBrowser(asset.Item);
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

            Settings.UI.MainWindow.Assets.Columns.Clear();
            Settings.UI.MainWindow.Assets.Columns.AddRange(Columns.Cast<AssetColumnSettings>());

            // Recreate the columns
            Columns = Settings.UI.MainWindow.Assets.Columns;
            m_columnsChanged = false;
        }

        /// <summary>
        /// When the assets update, update the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EveMonClient_CharacterAssetsUpdated(CharacterAssetsUpdatedEvent e)
        {
            try
            {
                Assets = Character.Assets;
                await UpdateContentAsync();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// When the conquerable station list updates, update the list.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private async void EveMonClient_ConquerableStationListUpdated()
        {
            try
            {
                if (Character == null)
                    return;

                await UpdateAssetLocationAsync();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// When the eve flags updates, update the list.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private async void EveMonClient_EveFlagsUpdated()
        {
            try
            {
                if (Character == null)
                    return;

                await UpdateContentAsync();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// When the character info updates, update the list.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Mainly to update the jumps from charater last known location to assets.</remarks>
        private async void EveMonClient_CharacterInfoUpdated(CharacterInfoUpdatedEvent e)
        {
            try
            {
                await UpdateAssetLocationAsync();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// Handles the SettingsChanged event of the EveMonClient control.
        /// </summary>
        private async void EveMonClient_SettingsChanged()
        {
            try
            {
                // No need to do this if control is not visible
                if (!Visible)
                    return;

                await UpdateContentAsync();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        /// <summary>
        /// Occurs when the item prices get updated.
        /// </summary>
        private async void EveMonClient_ItemPricesUpdated()
        {
            try
            {
                await UpdateContentAsync();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, true);
            }
        }

        #endregion
    }
}
