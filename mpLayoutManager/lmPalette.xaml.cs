namespace mpLayoutManager
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Windows;
    using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

    public partial class LmPalette
    {
        private const string LangItem = "mpLayoutManager";

        private ListViewDragDropManager<LayoutForBinding> _dragMgr;

        private static ObservableCollection<LayoutForBinding> _currentDocLayouts;

        private bool _showModel;

        public LmPalette()
        {
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources);
            ModPlusAPI.Windows.Helpers.WindowHelpers.ChangeStyleForResourceDictionary(Resources);
            Loaded += LmPalette_Loaded;
        }

        private void _dragMgr_ProcessDrop(object sender, ProcessDropEventArgs<LayoutForBinding> e)
        {
            try
            {
                var mdiActiveDocument = AcApp.DocumentManager.MdiActiveDocument;
                if (mdiActiveDocument != null)
                {
                    var newIndex = e.NewIndex;
                    var oldIndex = e.OldIndex;
                    if (e.ItemsSource[oldIndex] != null && e.ItemsSource[newIndex] != null)
                    {
                        if (!_showModel)
                        {
                            e.ItemsSource.Move(oldIndex, newIndex);
                            var num = 1;
                            foreach (var itemsSource in e.ItemsSource)
                            {
                                itemsSource.TabOrder = num;
                                num++;
                            }
                        }
                        else if (!(oldIndex == 0 | newIndex == -1 | newIndex == 0))
                        {
                            e.ItemsSource.Move(oldIndex, newIndex);
                            var num1 = 0;
                            foreach (var layoutForBinding in e.ItemsSource)
                            {
                                layoutForBinding.TabOrder = num1;
                                num1++;
                            }
                        }
                        else
                        {
                            return;
                        }

                        using (mdiActiveDocument.LockDocument())
                        {
                            using (var transaction = mdiActiveDocument.Database.TransactionManager.StartTransaction())
                            {
                                var current = LayoutManager.Current;
                                foreach (var itemsSource1 in e.ItemsSource)
                                {
                                    var layoutId = current.GetLayoutId(itemsSource1.LayoutName);
                                    var obj = transaction.GetObject(layoutId, OpenMode.ForWrite) as Layout;
                                    if (obj != null)
                                    {
                                        obj.TabOrder = itemsSource1.TabOrder;
                                    }
                                }

                                transaction.Commit();
                            }

                            mdiActiveDocument.Editor.Regen();
                        }

                        e.Effects = DragDropEffects.Move;
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void BindingLayoutsToListView()
        {
            try
            {
                LvLayouts.ItemsSource = null;
                var currentLayoutName = GetCurrentLayoutName();
                if (!string.IsNullOrEmpty(currentLayoutName))
                {
                    foreach (var currentDocLayout in _currentDocLayouts)
                    {
                        currentDocLayout.TabSelected = currentDocLayout.LayoutName.Equals(currentLayoutName);
                    }
                }

                LvLayouts.ItemsSource = _currentDocLayouts;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void BtAddLayout_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var mdiActiveDocument = AcApp.DocumentManager.MdiActiveDocument;
                var database = mdiActiveDocument != null ? mdiActiveDocument.Database : null;
                if (database != null)
                {
                    bool flag;
                    var flag1 = !bool.TryParse(UserConfigFile.GetValue("mpLayoutManager", "AskLayoutName"), out flag) | flag;
                    var current = LayoutManager.Current;
                    if (!flag1)
                    {
                        var str1 = string.Concat(ModPlusAPI.Language.GetItem(LangItem, "h3"), GetNewLayoutNumber(current));
                        using (mdiActiveDocument.LockDocument())
                        {
                            using (var transaction = mdiActiveDocument.TransactionManager.StartTransaction())
                            {
                                var objectId = current.CreateLayout(str1);
                                if (bool.TryParse(UserConfigFile.GetValue("mpLayoutManager", "OpenNewLayout"), out flag) & flag)
                                {
                                    var obj = transaction.GetObject(objectId, OpenMode.ForWrite) as Layout;
                                    if (obj != null)
                                    {
                                        obj.Initialize();
                                    }

                                    var layoutManager = current;
                                    string layoutName;
                                    if (obj != null)
                                    {
                                        layoutName = obj.LayoutName;
                                    }
                                    else
                                    {
                                        layoutName = null;
                                    }

                                    layoutManager.CurrentLayout = layoutName;
                                    database.TileMode = false;
                                    mdiActiveDocument.Editor.SwitchToPaperSpace();
                                }

                                GetCurrentDocLayouts();
                                transaction.Commit();
                                mdiActiveDocument.Editor.Regen();
                            }
                        }
                    }
                    else
                    {
                        var layoutNewName = new LayoutNewName
                        {
                            LayoutsNames = (
                                from layout in _currentDocLayouts
                                select layout.LayoutName).ToList(),
                            TbNewName =
                            {
                                Text = string.Concat(
                                    ModPlusAPI.Language.GetItem(LangItem, "h3"),
                                    GetNewLayoutNumber(current))
                            },
                            Topmost = true
                        };
                        var layoutNewName1 = layoutNewName;
                        var nullable = layoutNewName1.ShowDialog();
                        if (nullable.GetValueOrDefault() && nullable.HasValue)
                        {
                            using (mdiActiveDocument.LockDocument())
                            {
                                using (var transaction1 = mdiActiveDocument.TransactionManager.StartTransaction())
                                {
                                    var objectId1 = current.CreateLayout(layoutNewName1.TbNewName.Text);
                                    if (bool.TryParse(UserConfigFile.GetValue("mpLayoutManager", "OpenNewLayout"), out flag) & flag)
                                    {
                                        var obj1 = transaction1.GetObject(objectId1, OpenMode.ForWrite) as Layout;
                                        if (obj1 != null)
                                        {
                                            obj1.Initialize();
                                        }

                                        var layoutManager1 = current;
                                        var str = obj1 != null ? obj1.LayoutName : null;
                                        layoutManager1.CurrentLayout = str;
                                        database.TileMode = false;
                                        mdiActiveDocument.Editor.SwitchToPaperSpace();
                                    }

                                    GetCurrentDocLayouts();
                                    transaction1.Commit();
                                    mdiActiveDocument.Editor.Regen();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void BtAddLayoutByTemplate_OnClick(object sender, RoutedEventArgs e)
        {
            AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute("_.LAYOUT _T ", true, false, false);
        }

        private void DocumentManager_DocumentBecameCurrent(object sender, DocumentCollectionEventArgs e)
        {
            GetCurrentDocLayouts();
        }

        private void DocumentManager_DocumentDestroyed(object sender, DocumentDestroyedEventArgs e)
        {
            GetCurrentDocLayouts();
        }

        private string GetCopyLayoutName(LayoutForBinding selectedLayout, int copyCount)
        {
            var num = 1 + _currentDocLayouts.Count(currentDocLayout => currentDocLayout.LayoutName.Contains(selectedLayout.LayoutName)) + copyCount;
            return string.Concat(selectedLayout.LayoutName, " (", num, ")");
        }

        private void GetCurrentDocLayouts()
        {
            try
            {
                var mdiActiveDocument = AcApp.DocumentManager.MdiActiveDocument;
                Database database;
                if (mdiActiveDocument != null)
                {
                    database = mdiActiveDocument.Database;
                }
                else
                {
                    database = null;
                }

                var database1 = database;
                if (!((database1 == null) | (mdiActiveDocument == null)))
                {
                    _currentDocLayouts = new ObservableCollection<LayoutForBinding>();
                    using (var transaction = database1.TransactionManager.StartTransaction())
                    {
                        var current = LayoutManager.Current;
                        var obj = transaction.GetObject(database1.LayoutDictionaryId, OpenMode.ForRead, false) as DBDictionary;
                        if (obj != null)
                        {
                            foreach (var dBDictionaryEntry in obj)
                            {
                                var layout = transaction.GetObject(dBDictionaryEntry.Value, OpenMode.ForRead) as Layout;
                                if (layout != null)
                                {
                                    var layoutForBinding = new LayoutForBinding()
                                    {
                                        LayoutName = layout.LayoutName,
                                        ModelType = layout.ModelType,
                                        TabOrder = layout.TabOrder == -1 ? current.LayoutCount : layout.TabOrder,
                                        TabSelected = layout.TabSelected
                                    };
                                    if (_showModel)
                                    {
                                        _currentDocLayouts.Add(layoutForBinding);
                                    }
                                    else if (!layout.ModelType)
                                    {
                                        _currentDocLayouts.Add(layoutForBinding);
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                    }

                    _currentDocLayouts = new ObservableCollection<LayoutForBinding>(
                        from x in _currentDocLayouts
                        orderby x.TabOrder
                        select x);
                }
                else
                {
                    LvLayouts.ItemsSource = null;
                    return;
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }

            BindingLayoutsToListView();
        }

        private static string GetCurrentLayoutName()
        {
            try
            {
                return LayoutManager.Current?.CurrentLayout;
            }
            catch
            {
                return string.Empty;
            }
        }

        private int GetNewLayoutNumber(LayoutManager lm)
        {
            var layoutCount = lm.LayoutCount;
            var flag = true;
            while (flag)
            {
                var flag1 = false;
                foreach (var currentDocLayout in _currentDocLayouts)
                {
                    if (currentDocLayout.LayoutName.Equals(string.Concat("Лист", layoutCount)))
                    {
                        flag1 = true;
                    }
                }

                if (!flag1)
                {
                    flag = false;
                }
                else
                {
                    layoutCount++;
                }
            }

            return layoutCount;
        }

        private void ItemContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ContextMenu contextMenu)
                {
                    var placementTarget = contextMenu.PlacementTarget as ListViewItem;
                    var current = LayoutManager.Current;
                    foreach (var item in contextMenu.Items)
                    {
                        if (item is MenuItem menuItem)
                        {
                            if (LvLayouts.SelectedItems.Count > 1)
                            {
                                if ((
                                    from object selectedItem in LvLayouts.SelectedItems
                                    select selectedItem as LayoutForBinding).Any(
                                    si => si.ModelType))
                                {
                                    menuItem.IsEnabled = false;
                                }
                                else if (
                                    !(menuItem.Name.Equals("MiOpen") | menuItem.Name.Equals("MiRename") |
                                      menuItem.Name.Equals("MiPageSetup") | menuItem.Name.Equals("MiPlot")))
                                {
                                    menuItem.IsEnabled = true;
                                }
                                else
                                {
                                    menuItem.IsEnabled = false;
                                }
                            }
                            else
                            {
                                if (LvLayouts.SelectedItem is LayoutForBinding layoutForBinding && LvLayouts.SelectedItems.Count == 1 &
                                    layoutForBinding.ModelType)
                                {
                                    if (!(menuItem.Name.Equals("MiOpen") | menuItem.Name.Equals("MiSelectAll")))
                                    {
                                        menuItem.IsEnabled = false;
                                    }
                                    else
                                    {
                                        menuItem.IsEnabled = true;
                                    }
                                }
                                else if (
                                    placementTarget != null && current.CurrentLayout.Equals(
                                        ((LayoutForBinding)placementTarget.Content).LayoutName))
                                {
                                    menuItem.IsEnabled = true;
                                }
                                else if (menuItem.Name.Equals("MiPageSetup") | menuItem.Name.Equals("MiPlot") |
                                         menuItem.Name.Equals("MiExportLayout"))
                                {
                                    menuItem.IsEnabled = false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void LayoutItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _dragMgr.ListView = null;
                OpenSelectedLayout();
                _dragMgr.ListView = LvLayouts;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void Lm_LayoutCopied(object sender, LayoutCopiedEventArgs e)
        {
            GetCurrentDocLayouts();
        }

        private void Lm_LayoutCreated(object sender, LayoutEventArgs e)
        {
            GetCurrentDocLayouts();
        }

        private void Lm_LayoutRemoved(object sender, LayoutEventArgs e)
        {
            GetCurrentDocLayouts();
        }

        private void Lm_LayoutRenamed(object sender, LayoutRenamedEventArgs e)
        {
            GetCurrentDocLayouts();
        }

        private void Lm_LayoutsReordered(object sender, EventArgs e)
        {
            GetCurrentDocLayouts();
        }

        private void Lm_LayoutSwitched(object sender, LayoutEventArgs e)
        {
            BindingLayoutsToListView();
        }

        private void LmPalette_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _showModel = !bool.TryParse(UserConfigFile.GetValue("mpLayoutManager", "ShowModel"), out var flag) | flag;
                _dragMgr = new ListViewDragDropManager<LayoutForBinding>(LvLayouts)
                {
                    DragAdornerOpacity = 1
                };
                _dragMgr.ProcessDrop += _dragMgr_ProcessDrop;
                LvLayouts.DragEnter += LvLayouts_DragEnter;
                GetCurrentDocLayouts();
                var current = LayoutManager.Current;
                current.LayoutCreated += Lm_LayoutCreated;
                current.LayoutRenamed += Lm_LayoutRenamed;
                current.LayoutRemoved += Lm_LayoutRemoved;
                current.LayoutCopied += Lm_LayoutCopied;
                current.LayoutsReordered += Lm_LayoutsReordered;
                current.LayoutSwitched += Lm_LayoutSwitched;
                AcApp.DocumentManager.DocumentBecameCurrent += DocumentManager_DocumentBecameCurrent;
                AcApp.DocumentManager.DocumentDestroyed += DocumentManager_DocumentDestroyed;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void LmSettings_OnClick(object sender, RoutedEventArgs e)
        {
            var lmSetting = new LmSettings()
            {
                Topmost = true
            };
            lmSetting.ShowDialog();
            if (lmSetting.ChkShowModel.IsChecked.HasValue)
            {
                _showModel = lmSetting.ChkShowModel.IsChecked.Value;
            }

            if (!lmSetting.ChkAddToMpPalette.IsChecked ?? true)
            {
                FunctionStart.RemoveFromMpPalette(true);
            }
            else
            {
                FunctionStart.AddToMpPalette(true);
            }

            GetCurrentDocLayouts();
            BindingLayoutsToListView();
        }

        private void LvLayouts_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
        }

        private void LvLayouts_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null & LvLayouts != null)
            {
                var listViewDragDropManager = _dragMgr;
                ListView lvLayouts;
                if (e.AddedItems.Count > 1)
                {
                    lvLayouts = null;
                }
                else
                {
                    lvLayouts = LvLayouts;
                }

                listViewDragDropManager.ListView = lvLayouts;
            }
        }

        private void MenuItem_Delete_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var mdiActiveDocument = AcApp.DocumentManager.MdiActiveDocument;
                if (LvLayouts.SelectedItems.Count == 1)
                {
                    if (LvLayouts.SelectedItem is LayoutForBinding selectedItem &&
                        ModPlusAPI.Windows.MessageBox.ShowYesNo(
                            string.Concat(
                            ModPlusAPI.Language.GetItem(LangItem, "h4"),
                            Environment.NewLine, selectedItem.LayoutName, "?"), MessageBoxIcon.Question))
                    {
                        using (mdiActiveDocument.LockDocument())
                        {
                            LayoutManager.Current.DeleteLayout(selectedItem.LayoutName);
                            _currentDocLayouts.Remove(selectedItem);
                            BindingLayoutsToListView();
                            mdiActiveDocument.Editor.Regen();
                        }
                    }
                }
                else if (LvLayouts.SelectedItems.Count > 1)
                {
                    if (ModPlusAPI.Windows.MessageBox.ShowYesNo(ModPlusAPI.Language.GetItem(LangItem, "h5"), MessageBoxIcon.Question))
                    {
                        var list = (
                            from selectedLayout in LvLayouts.SelectedItems.OfType<LayoutForBinding>()
                            where !selectedLayout.ModelType
                            select selectedLayout).ToList();
                        if (list.Count > 0)
                        {
                            using (mdiActiveDocument.LockDocument())
                            {
                                foreach (var layoutForBinding in list)
                                {
                                    LayoutManager.Current.DeleteLayout(layoutForBinding.LayoutName);
                                    _currentDocLayouts.Remove(layoutForBinding);
                                }
                            }

                            BindingLayoutsToListView();
                            mdiActiveDocument.Editor.Regen();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void MenuItem_Open_OnClick(object sender, RoutedEventArgs e)
        {
            OpenSelectedLayout();
        }

        private void MenuItem_Rename_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var mdiActiveDocument = AcApp.DocumentManager.MdiActiveDocument;
                if (LvLayouts.SelectedItems.Count <= 1)
                {
                    if (LvLayouts.SelectedItem is LayoutForBinding selectedItem)
                    {
                        if (!selectedItem.ModelType)
                        {
                            var renameLayout = new RenameLayout
                            {
                                LayoutsNames = (
                                    from layout in _currentDocLayouts
                                    select layout.LayoutName).ToList(),
                                TbCurrentName = { Text = selectedItem.LayoutName },
                                TbNewName = { Text = selectedItem.LayoutName },
                                Topmost = true
                            };
                            var renameLayout1 = renameLayout;
                            var nullable = renameLayout1.ShowDialog();
                            if (nullable.GetValueOrDefault() && nullable.HasValue)
                            {
                                if (renameLayout1.TbNewName.Text != renameLayout1.TbCurrentName.Text)
                                {
                                    using (var transaction = mdiActiveDocument.TransactionManager.StartTransaction())
                                    {
                                        using (mdiActiveDocument.LockDocument())
                                        {
                                            var current = LayoutManager.Current;
                                            current.RenameLayout(renameLayout1.TbCurrentName.Text, renameLayout1.TbNewName.Text);
                                            GetCurrentDocLayouts();
                                        }

                                        transaction.Commit();
                                        mdiActiveDocument.Editor.Regen();
                                    }
                                }
                            }
                        }
                        else
                        {
                            ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "h6"));
                        }
                    }
                }
                else
                {
                    ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "h7"));
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void MiExportLayout_OnClick(object sender, RoutedEventArgs e)
        {
            AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute("_.EXPORTLAYOUT ", true, false, false);
        }

        private void MiMoveCopy_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var mdiActiveDocument = AcApp.DocumentManager.MdiActiveDocument;
                var current = LayoutManager.Current;
                var selectedItems = LvLayouts.SelectedItems;
                if (selectedItems != null)
                {
                    if (selectedItems.Count != 0)
                    {
                        if (!selectedItems.Cast<object>().Any(selectedItem =>
                        {
                            var layoutForBinding = selectedItem as LayoutForBinding;
                            return layoutForBinding?.ModelType ?? false;
                        }))
                        {
                            var moveCopyLayout = new MoveCopyLayout()
                            {
                                Topmost = true
                            };
                            var isChecked = moveCopyLayout.ShowDialog();
                            if (isChecked.GetValueOrDefault() && isChecked.HasValue)
                            {
                                isChecked = moveCopyLayout.ChkMakeCopy.IsChecked;
                                bool value;
                                if (!isChecked.HasValue)
                                {
                                    value = false;
                                }
                                else
                                {
                                    isChecked = moveCopyLayout.ChkMakeCopy.IsChecked;
                                    value = isChecked.Value;
                                }

                                if (!value)
                                {
                                    if (moveCopyLayout.SelectedLayoutTabOrder != -1)
                                    {
                                        var num = _currentDocLayouts.IndexOf(_currentDocLayouts.Single(x => x.LayoutName.Equals(moveCopyLayout.SelectedLayoutName)));
                                        for (var i = selectedItems.Count - 1; i >= 0; i--)
                                        {
                                            var item = selectedItems[i] as LayoutForBinding;
                                            _currentDocLayouts.Move(_currentDocLayouts.IndexOf(item), num);
                                        }

                                        SetNewTabOrderByListPositions();
                                    }
                                    else
                                    {
                                        foreach (LayoutForBinding layoutForBinding1 in selectedItems)
                                        {
                                            _currentDocLayouts.Move(_currentDocLayouts.IndexOf(layoutForBinding1), _currentDocLayouts.Count - 1);
                                        }

                                        SetNewTabOrderByListPositions();
                                    }
                                }
                                else if (moveCopyLayout.SelectedLayoutTabOrder != -1)
                                {
                                    var num1 = _currentDocLayouts.IndexOf(_currentDocLayouts.Single(x => x.LayoutName.Equals(moveCopyLayout.SelectedLayoutName)));
                                    current.LayoutCreated -= Lm_LayoutCreated;
                                    current.LayoutCopied -= Lm_LayoutCopied;
                                    using (mdiActiveDocument.LockDocument())
                                    {
                                        for (var j = selectedItems.Count - 1; j >= 0; j--)
                                        {
                                            for (var i = 0; i < moveCopyLayout.NuCopyCount.Value; i++)
                                            {
                                                if (selectedItems[j] is LayoutForBinding selectedLayout)
                                                {
                                                    if (num1 == 1)
                                                    {
                                                        current.CloneLayout(selectedLayout.LayoutName, GetCopyLayoutName(selectedLayout, i), 1 + i);
                                                    }
                                                    else
                                                    {
                                                        current.CloneLayout(selectedLayout.LayoutName, GetCopyLayoutName(selectedLayout, i), num1 - 1 + i);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    current.LayoutCreated += Lm_LayoutCreated;
                                    current.LayoutCopied += Lm_LayoutCopied;
                                    mdiActiveDocument.Editor.Regen();
                                    GetCurrentDocLayouts();
                                }
                                else
                                {
                                    current.LayoutCreated -= Lm_LayoutCreated;
                                    current.LayoutCopied -= Lm_LayoutCopied;
                                    using (mdiActiveDocument.LockDocument())
                                    {
                                        foreach (LayoutForBinding layoutForBinding2 in selectedItems)
                                        {
                                            for (var i = 0; i < moveCopyLayout.NuCopyCount.Value; i++)
                                            {
                                                current.CloneLayout(layoutForBinding2.LayoutName, GetCopyLayoutName(layoutForBinding2, i), current.LayoutCount + i);
                                            }
                                        }

                                        mdiActiveDocument.Editor.Regen();
                                    }

                                    current.LayoutCreated += Lm_LayoutCreated;
                                    current.LayoutCopied += Lm_LayoutCopied;
                                    GetCurrentDocLayouts();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void MiPageSetup_OnClick(object sender, RoutedEventArgs e)
        {
            AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute("_.PAGESETUP ", true, false, false);
        }

        private void MiPlot_OnClick(object sender, RoutedEventArgs e)
        {
            AcApp.DocumentManager.MdiActiveDocument.SendStringToExecute("_.PLOT ", true, false, false);
        }

        private void MiSelectAll_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_showModel)
                {
                    LvLayouts.SelectAll();
                }
                else
                {
                    for (var i = 0; i < LvLayouts.Items.Count; i++)
                    {
                        if (LvLayouts.ItemContainerGenerator.ContainerFromIndex(i) is ListViewItem listViewItem)
                        {
                            listViewItem.IsSelected = i != 0;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void OpenSelectedLayout()
        {
            var current = LayoutManager.Current;
            try
            {
                try
                {
                    var mdiActiveDocument = AcApp.DocumentManager.MdiActiveDocument;
                    if (LvLayouts.SelectedItems.Count <= 1)
                    {
                        current.LayoutSwitched -= Lm_LayoutSwitched;
                        LvLayouts.SelectionChanged -= LvLayouts_OnSelectionChanged;
                        if (LvLayouts.SelectedItem is LayoutForBinding selectedItem)
                        {
                            using (mdiActiveDocument.LockDocument())
                            {
                                current.CurrentLayout = selectedItem.LayoutName;
                                mdiActiveDocument.Editor.Regen();
                            }

                            BindingLayoutsToListView();
                        }
                    }
                    else
                    {
                        ModPlusAPI.Windows.MessageBox.Show(ModPlusAPI.Language.GetItem(LangItem, "h7"));
                    }
                }
                catch (Exception exception)
                {
                    ExceptionBox.Show(exception);
                }
            }
            finally
            {
                current.LayoutSwitched += Lm_LayoutSwitched;
                LvLayouts.SelectionChanged += LvLayouts_OnSelectionChanged;
            }
        }

        private void SetNewTabOrderByListPositions()
        {
            var mdiActiveDocument = AcApp.DocumentManager.MdiActiveDocument;
            if (mdiActiveDocument != null)
            {
                using (mdiActiveDocument.LockDocument())
                {
                    using (var transaction = mdiActiveDocument.Database.TransactionManager.StartTransaction())
                    {
                        var current = LayoutManager.Current;
                        var num = 0;
                        if (!_showModel)
                        {
                            num = 1;
                        }

                        foreach (var currentDocLayout in _currentDocLayouts)
                        {
                            var layoutId = current.GetLayoutId(currentDocLayout.LayoutName);
                            var obj = transaction.GetObject(layoutId, OpenMode.ForWrite) as Layout;
                            if (obj != null)
                            {
                                obj.TabOrder = num;
                            }

                            num++;
                        }

                        transaction.Commit();
                    }

                    mdiActiveDocument.Editor.Regen();
                }
            }
        }

        private class LayoutForBinding
        {
            public string LayoutName
            {
                get;
                set;
            }

            public bool ModelType
            {
                get;
                set;
            }

            public int TabOrder
            {
                get;
                set;
            }

            public bool TabSelected
            {
                get;
                set;
            }
        }
    }
}