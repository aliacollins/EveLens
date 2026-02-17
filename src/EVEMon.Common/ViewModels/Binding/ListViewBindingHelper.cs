using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace EVEMon.Common.ViewModels.Binding
{
    /// <summary>
    /// Binds a <see cref="ListViewModel{TItem,TColumn,TGrouping}"/>'s GroupedItems to a WinForms <see cref="ListView"/>.
    /// Handles column creation, group creation, item population, and sort toggling.
    /// </summary>
    public static class ListViewBindingHelper
    {
        /// <summary>
        /// Binds a ListViewModel's GroupedItems to a ListView. When GroupedItems changes,
        /// the ListView is repopulated. Column clicks toggle sorting on the VM.
        /// </summary>
        /// <typeparam name="TItem">The domain model type.</typeparam>
        /// <typeparam name="TColumn">The column enum type.</typeparam>
        /// <typeparam name="TGrouping">The grouping enum type.</typeparam>
        /// <param name="viewModel">The ViewModel to bind.</param>
        /// <param name="listView">The target ListView control.</param>
        /// <param name="createListViewItem">Function that creates a ListViewItem from a domain item.</param>
        /// <param name="getColumnFromTag">Function to extract the column enum from a ColumnHeader's Tag.</param>
        /// <returns>An <see cref="IDisposable"/> that removes the binding when disposed.</returns>
        public static IDisposable Bind<TItem, TColumn, TGrouping>(
            ListViewModel<TItem, TColumn, TGrouping> viewModel,
            ListView listView,
            Func<TItem, ListViewItem> createListViewItem,
            Func<ColumnHeader, TColumn> getColumnFromTag)
            where TColumn : struct, Enum
            where TGrouping : struct, Enum
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            if (listView == null) throw new ArgumentNullException(nameof(listView));
            if (createListViewItem == null) throw new ArgumentNullException(nameof(createListViewItem));
            if (getColumnFromTag == null) throw new ArgumentNullException(nameof(getColumnFromTag));

            var composite = new CompositeDisposable();

            // Wire column click -> ToggleSort
            ColumnClickEventHandler columnClickHandler = (sender, e) =>
            {
                if (e.Column >= 0 && e.Column < listView.Columns.Count)
                {
                    var column = getColumnFromTag(listView.Columns[e.Column]);
                    viewModel.ToggleSort(column);
                }
            };
            listView.ColumnClick += columnClickHandler;
            composite.Add(new ActionDisposable(() => listView.ColumnClick -= columnClickHandler));

            // Wire GroupedItems property changes -> repopulate ListView
            PropertyChangedEventHandler propertyChanged = (sender, e) =>
            {
                if (e.PropertyName != nameof(ListViewModel<TItem, TColumn, TGrouping>.GroupedItems))
                    return;

                PopulateListView(viewModel, listView, createListViewItem);
            };
            viewModel.PropertyChanged += propertyChanged;
            composite.Add(new ActionDisposable(() => viewModel.PropertyChanged -= propertyChanged));

            return composite;
        }

        /// <summary>
        /// Populates the ListView from the ViewModel's current GroupedItems.
        /// </summary>
        private static void PopulateListView<TItem, TColumn, TGrouping>(
            ListViewModel<TItem, TColumn, TGrouping> viewModel,
            ListView listView,
            Func<TItem, ListViewItem> createListViewItem)
            where TColumn : struct, Enum
            where TGrouping : struct, Enum
        {
            void DoPopulate()
            {
                listView.BeginUpdate();
                try
                {
                    listView.Items.Clear();
                    listView.Groups.Clear();

                    var groups = viewModel.GroupedItems;
                    if (groups == null || groups.Count == 0)
                        return;

                    bool hasGrouping = groups.Count > 1 || !string.IsNullOrEmpty(groups[0].Key);

                    foreach (var group in groups)
                    {
                        ListViewGroup? lvGroup = null;
                        if (hasGrouping)
                        {
                            lvGroup = new ListViewGroup(group.Key);
                            listView.Groups.Add(lvGroup);
                        }

                        foreach (var item in group.Items)
                        {
                            var lvItem = createListViewItem(item);
                            if (lvGroup != null)
                                lvItem.Group = lvGroup;
                            listView.Items.Add(lvItem);
                        }
                    }
                }
                finally
                {
                    listView.EndUpdate();
                }
            }

            if (listView.InvokeRequired)
            {
                try
                {
                    listView.BeginInvoke(new Action(DoPopulate));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
            else
            {
                DoPopulate();
            }
        }
    }
}
