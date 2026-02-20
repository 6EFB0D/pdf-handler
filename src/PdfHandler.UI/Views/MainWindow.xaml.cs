// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PdfHandler.Core.Models;
using System.Collections.ObjectModel;
using PdfHandler.Core.Interfaces;

namespace PdfHandler.UI.Views;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        // ViewModelのRootFolderプロパティ変更を監視
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // 初期状態を設定（既にRootFolderが設定されている場合）
        // ただし、InitializeAsyncが非同期で実行されるため、少し遅延して確認
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_viewModel.RootFolder != null)
            {
                var items = new ObservableCollection<FolderNode> { _viewModel.RootFolder };
                FolderTreeView.ItemsSource = items;
            }

            // プレビューの初期状態を反映
            UpdatePreviewColumnWidth(_viewModel.IsPreviewVisible);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
    }
    
    private void UpdatePreviewColumnWidth(bool isVisible)
    {
        if (MainContentGrid?.ColumnDefinitions.Count >= 5)
        {
            var previewColumn = MainContentGrid.ColumnDefinitions[4];
            var thumbnailColumn = MainContentGrid.ColumnDefinitions[2];
            var splitterColumn = MainContentGrid.ColumnDefinitions[3];
            
            if (isVisible)
            {
                // プレビューを表示：プレビュー列を*に、サムネイル列を固定幅に
                previewColumn.Width = new GridLength(1, GridUnitType.Star);
                thumbnailColumn.Width = new GridLength(400);
                splitterColumn.Width = new GridLength(5);
            }
            else
            {
                // プレビューを非表示：プレビュー列を0に、サムネイル列を*に
                previewColumn.Width = new GridLength(0);
                thumbnailColumn.Width = new GridLength(1, GridUnitType.Star);
                splitterColumn.Width = new GridLength(0);
            }
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.RootFolder))
        {
            // UIスレッドで実行
            Dispatcher.Invoke(() =>
            {
                // TreeViewのItemsSourceを手動で設定
                if (_viewModel?.RootFolder != null)
                {
                    var items = new ObservableCollection<FolderNode> { _viewModel.RootFolder };
                    FolderTreeView.ItemsSource = items;
                    
                    // 選択されたフォルダをTreeViewで選択状態にする
                    if (_viewModel.SelectedFolder != null)
                    {
                        SelectFolderInTreeView(_viewModel.SelectedFolder);
                    }
                }
                else
                {
                    // RootFolderがnullの場合は空のコレクションを設定
                    FolderTreeView.ItemsSource = new ObservableCollection<FolderNode>();
                }
            });
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.SelectedFolder) && _viewModel?.SelectedFolder != null)
        {
            // TreeViewで選択状態にする
            Dispatcher.Invoke(() =>
            {
                SelectFolderInTreeView(_viewModel.SelectedFolder!);
            });
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsPreviewVisible))
        {
            // プレビューの表示/非表示に応じて列の幅を更新
            Dispatcher.Invoke(() =>
            {
                UpdatePreviewColumnWidth(_viewModel?.IsPreviewVisible ?? true);
            });
        }
    }
    
    private void SelectFolderInTreeView(FolderNode folder)
    {
        // TreeViewでフォルダを選択状態にする
        // SelectedItemは読み取り専用なので、ViewModelのSelectedFolderプロパティを設定することで
        // XAMLのバインディングで選択状態が反映される
        if (FolderTreeView.ItemsSource is ObservableCollection<FolderNode> items && items.Count > 0)
        {
            var rootNode = items[0];
            var targetNode = FindFolderNode(rootNode, folder.Path);
            if (targetNode != null)
            {
                // 親ノードを展開
                ExpandParentNodes(targetNode);
                // ViewModelのSelectedFolderを設定（これによりTreeViewの選択状態も更新される）
                if (_viewModel != null)
                {
                    _viewModel.SelectedFolder = targetNode;
                }
            }
        }
    }
    
    private FolderNode? FindFolderNode(FolderNode node, string targetPath)
    {
        if (node.Path.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
        {
            return node;
        }
        
        foreach (var child in node.Children)
        {
            var found = FindFolderNode(child, targetPath);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }
    
    private void ExpandParentNodes(FolderNode node)
    {
        // 親ノードを展開するために、ルートから該当ノードまでのパスを展開
        if (FolderTreeView.ItemsSource is ObservableCollection<FolderNode> items && items.Count > 0)
        {
            var rootNode = items[0];
            ExpandPathToNode(rootNode, node.Path);
        }
    }
    
    private bool ExpandPathToNode(FolderNode node, string targetPath)
    {
        if (node.Path.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
        {
            node.IsExpanded = true;
            return true;
        }
        
        if (targetPath.StartsWith(node.Path, StringComparison.OrdinalIgnoreCase))
        {
            node.IsExpanded = true;
            foreach (var child in node.Children)
            {
                if (ExpandPathToNode(child, targetPath))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_viewModel != null && e.NewValue is FolderNode node)
        {
            _viewModel.SelectedFolder = node;
        }
    }

    private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TreeViewItem item && item.DataContext is FolderNode node)
        {
            // 遅延読み込み：展開時に子フォルダを読み込む
            if (!node.IsChildrenLoaded && _viewModel != null)
            {
                await _viewModel.LoadChildrenAsync(node);
            }
        }
    }

    private FolderNode? _contextMenuFolderNode;

    private async void FolderTreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is TreeView treeView && _viewModel != null)
        {
            // 右クリックされたアイテムを取得
            var mousePosition = Mouse.GetPosition(treeView);
            var hitTestResult = VisualTreeHelper.HitTest(treeView, mousePosition);
            if (hitTestResult != null)
            {
                var item = FindParent<System.Windows.Controls.TreeViewItem>(hitTestResult.VisualHit);
                if (item != null && item.DataContext is FolderNode clickedNode)
                {
                    // 選択状態にする（SelectedFolderプロパティを設定）
                    _viewModel.SelectedFolder = clickedNode;
                    _contextMenuFolderNode = clickedNode;
                    item.IsSelected = true;

                    // 遅延読み込み
                    if (!clickedNode.IsChildrenLoaded)
                    {
                        await _viewModel.LoadChildrenAsync(clickedNode);
                    }

                    // お気に入りかどうかを確認してメニューを動的に設定
                    if (treeView.ContextMenu is ContextMenu contextMenu)
                    {
                        var favorites = await _viewModel.GetFavoritesAsync();
                        var isFavorite = favorites.Any(f => f.Path.Equals(clickedNode.Path, StringComparison.OrdinalIgnoreCase));
                        var isEmptyPath = string.IsNullOrEmpty(clickedNode.Path);

                        if (contextMenu.FindName("AddFavoriteMenuItem") is MenuItem addMenuItem)
                        {
                            addMenuItem.IsEnabled = !isEmptyPath && !isFavorite;
                        }
                        if (contextMenu.FindName("RemoveFavoriteMenuItem") is MenuItem removeMenuItem)
                        {
                            removeMenuItem.IsEnabled = !isEmptyPath && isFavorite;
                        }
                        if (contextMenu.FindName("RenameFavoriteMenuItem") is MenuItem renameMenuItem)
                        {
                            renameMenuItem.IsEnabled = !isEmptyPath && isFavorite;
                        }
                    }
                }
            }
        }
    }

    private async void AddFavoriteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _contextMenuFolderNode != null)
        {
            await _viewModel.AddFavoriteAsync(_contextMenuFolderNode);
        }
    }

    private async void RemoveFavoriteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _contextMenuFolderNode != null)
        {
            await _viewModel.RemoveFavoriteFromFolderAsync(_contextMenuFolderNode);
        }
    }

    private async void RenameFavoriteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _contextMenuFolderNode != null)
        {
            await _viewModel.RenameFavoriteAsync(_contextMenuFolderNode);
        }
    }

    private PdfFileInfo? _contextMenuPdfFile;

    private async void RotateRightMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _contextMenuPdfFile != null)
        {
            // サムネイルで表示しているページ番号を取得（リストビューの場合は1ページ目）
            int pageNumber = _viewModel.IsThumbnailView ? _contextMenuPdfFile.DisplayPageNumber : 1;
            await _viewModel.RotatePdfPageAsync(_contextMenuPdfFile, pageNumber, 90);
        }
    }

    private async void RotateLeftMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _contextMenuPdfFile != null)
        {
            // サムネイルで表示しているページ番号を取得（リストビューの場合は1ページ目）
            int pageNumber = _viewModel.IsThumbnailView ? _contextMenuPdfFile.DisplayPageNumber : 1;
            await _viewModel.RotatePdfPageAsync(_contextMenuPdfFile, pageNumber, 270);
        }
    }

    private async void Rotate180MenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _contextMenuPdfFile != null)
        {
            // サムネイルで表示しているページ番号を取得（リストビューの場合は1ページ目）
            int pageNumber = _viewModel.IsThumbnailView ? _contextMenuPdfFile.DisplayPageNumber : 1;
            await _viewModel.RotatePdfPageAsync(_contextMenuPdfFile, pageNumber, 180);
        }
    }

    private async void ThumbnailPreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PdfFileInfo fileInfo && _viewModel != null)
        {
            if (fileInfo.DisplayPageNumber > 1)
            {
                fileInfo.DisplayPageNumber--;
                // サムネイルを更新
                await _viewModel.LoadThumbnailPageAsync(fileInfo, fileInfo.DisplayPageNumber);
            }
        }
    }

    private async void ThumbnailNextPage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PdfFileInfo fileInfo && _viewModel != null)
        {
            if (fileInfo.DisplayPageNumber < fileInfo.PageCount)
            {
                fileInfo.DisplayPageNumber++;
                // サムネイルを更新
                await _viewModel.LoadThumbnailPageAsync(fileInfo, fileInfo.DisplayPageNumber);
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        // AboutDialogを表示
        var aboutDialog = new AboutDialog();
        aboutDialog.Owner = this;
        aboutDialog.ShowDialog();
    }

    private void License_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LicenseDialog { Owner = this };
        dialog.ShowDialog();
        _viewModel?.UpdateTrialStatus();
    }

    // インライン編集機能
    private void FileListView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2 && _viewModel?.SelectedPdfFile != null)
        {
            // ライセンスチェック
            var app = (App)Application.Current;
            var licenseService = app.GetService<ILicenseService>();
            if (!licenseService.CanUseRename())
            {
                MessageBox.Show(
                    "ファイル名変更機能は有償版の機能です。\n\n14日間の試用期間中は全機能をご利用いただけます。\n試用期間が終了した場合は、ライセンスの購入が必要です。",
                    "機能制限",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // ライセンスダイアログを表示
                var licenseDialog = new LicenseDialog { Owner = this };
                licenseDialog.ShowDialog();
                e.Handled = true;
                return;
            }
            
            StartInlineEdit(_viewModel.SelectedPdfFile);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            DeleteFiles_Click(sender, e);
            e.Handled = true;
        }
    }

    private void FileName_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is TextBlock textBlock && textBlock.DataContext is PdfFileInfo file)
        {
            // ライセンスチェック
            var app = (App)Application.Current;
            var licenseService = app.GetService<ILicenseService>();
            if (!licenseService.CanUseRename())
            {
                MessageBox.Show(
                    "ファイル名変更機能は有償版の機能です。\n\n14日間の試用期間中は全機能をご利用いただけます。\n試用期間が終了した場合は、ライセンスの購入が必要です。",
                    "機能制限",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // ライセンスダイアログを表示
                var licenseDialog = new LicenseDialog { Owner = this };
                licenseDialog.ShowDialog();
                e.Handled = true;
                return;
            }
            
            StartInlineEdit(file);
            e.Handled = true;
        }
    }

    private void StartInlineEdit(PdfFileInfo file)
    {
        // ライセンスチェック（念のため）
        var app = (App)Application.Current;
        var licenseService = app.GetService<ILicenseService>();
        if (!licenseService.CanUseRename())
        {
            return;
        }
        
        file.EditingName = Path.GetFileNameWithoutExtension(file.FileName);
        file.IsEditing = true;
    }

    private void FileNameEdit_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    private void FileNameEdit_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not PdfFileInfo file)
            return;

        if (e.Key == Key.Enter)
        {
            CommitEdit(file);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelEdit(file);
            e.Handled = true;
        }
    }

    private void FileNameEdit_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is PdfFileInfo file)
        {
            CommitEdit(file);
        }
    }

    private async void CommitEdit(PdfFileInfo file)
    {
        if (!file.IsEditing) return;

        var newName = file.EditingName.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            CancelEdit(file);
            return;
        }

        // 拡張子を追加
        if (!newName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            newName += ".pdf";
        }

        // ファイル名が変更されていない場合
        if (newName.Equals(file.FileName, StringComparison.OrdinalIgnoreCase))
        {
            CancelEdit(file);
            return;
        }

        // 不正な文字チェック
        var invalidChars = Path.GetInvalidFileNameChars();
        if (newName.Any(c => invalidChars.Contains(c)))
        {
            MessageBox.Show("ファイル名に使用できない文字が含まれています。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        file.IsEditing = false;

        // ファイル名を変更
        var directory = Path.GetDirectoryName(file.FilePath);
        var newPath = Path.Combine(directory!, newName);

        if (_viewModel != null)
        {
            var fileService = _viewModel.GetType().GetField("_fileService",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(_viewModel) as Core.Interfaces.IFileService;

            if (fileService != null)
            {
                var success = await fileService.RenameFileAsync(file.FilePath, newPath);
                if (success)
                {
                    // RefreshCommandを実行
                    if (_viewModel.RefreshCommand.CanExecute(null))
                    {
                        await _viewModel.RefreshCommand.ExecuteAsync(null);
                    }
                    _viewModel.StatusText = "ファイル名を変更しました";
                }
                else
                {
                    MessageBox.Show("ファイル名の変更に失敗しました。", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void CancelEdit(PdfFileInfo file)
    {
        file.IsEditing = false;
        file.EditingName = string.Empty;
    }

    // サムネイルダブルクリックでファイルを開く
    private void ThumbnailItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel?.SelectedPdfFile != null && e.ChangedButton == MouseButton.Left)
        {
            try
            {
                // デフォルトのアプリケーションでファイルを開く
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _viewModel.SelectedPdfFile.FilePath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルを開けませんでした: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 取扱説明書を表示
    private void ShowUserManual_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // USER_MANUAL.mdを開く
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string manualPath = Path.Combine(appDir, "USER_MANUAL.md");

            // ファイルが存在しない場合はプロジェクトルートから探す
            if (!File.Exists(manualPath))
            {
                // 開発時用のパス
                string projectRoot = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", ".."));
                manualPath = Path.Combine(projectRoot, "USER_MANUAL.md");
            }

            if (File.Exists(manualPath))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = manualPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            else
            {
                MessageBox.Show(
                    "取扱説明書が見つかりません。\n\nGitHubリポジトリのUSER_MANUAL.mdを参照してください。",
                    "情報",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"取扱説明書を開けませんでした: {ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // PDF結合（複数選択対応）
    private async void MergePdfs_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // 現在のビューから選択されたアイテムを順序を保って取得
        var selectedFiles = new List<string>();

        if (_viewModel.IsThumbnailView && ThumbnailListView.SelectedItems.Count > 0)
        {
            // Items全体を走査して、選択されているものだけを順番に追加
            foreach (PdfFileInfo item in ThumbnailListView.Items)
            {
                if (ThumbnailListView.SelectedItems.Contains(item))
                {
                    selectedFiles.Add(item.FilePath);
                }
            }
        }
        else if (!_viewModel.IsThumbnailView && FileListView.SelectedItems.Count > 0)
        {
            // Items全体を走査して、選択されているものだけを順番に追加
            foreach (PdfFileInfo item in FileListView.Items)
            {
                if (FileListView.SelectedItems.Contains(item))
                {
                    selectedFiles.Add(item.FilePath);
                }
            }
        }

        if (selectedFiles.Count > 0)
        {
            await _viewModel.MergePdfsWithFilesAsync(selectedFiles);
        }
        else
        {
            MessageBox.Show("結合するPDFファイルを選択してください。\n\n複数選択: Ctrlキーを押しながらクリック", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // ファイル削除（複数選択対応）
    private async void DeleteFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;

        // 現在のビューから選択されたアイテムを取得
        var selectedFiles = new List<PdfFileInfo>();

        if (_viewModel.IsThumbnailView && ThumbnailListView.SelectedItems.Count > 0)
        {
            foreach (PdfFileInfo item in ThumbnailListView.SelectedItems)
            {
                selectedFiles.Add(item);
            }
        }
        else if (!_viewModel.IsThumbnailView && FileListView.SelectedItems.Count > 0)
        {
            foreach (PdfFileInfo item in FileListView.SelectedItems)
            {
                selectedFiles.Add(item);
            }
        }

        if (selectedFiles.Count == 0)
        {
            MessageBox.Show("削除するPDFファイルを選択してください。\n\n複数選択: Ctrlキーを押しながらクリック", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 確認ダイアログ
        var message = selectedFiles.Count == 1
            ? $"ファイル '{Path.GetFileName(selectedFiles[0].FilePath)}' を削除しますか？"
            : $"{selectedFiles.Count}個のファイルを削除しますか？";

        message += "\n\nこの操作は元に戻せません。";

        var result = MessageBox.Show(message, "確認",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        // ファイルを削除
        await _viewModel.DeleteFilesAsync(selectedFiles);
    }

    private void OpenBinderManager_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BinderManagerDialog();
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void PrintDriverSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDriverSettingsDialog();
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void FileListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // 右クリックメニューが開かれた時に、選択されたファイルを更新
        if (sender is ListView listView)
        {
            // 現在選択されているアイテムがあれば、それをViewModelに設定
            if (listView.SelectedItem is PdfFileInfo selectedFile && _viewModel != null)
            {
                _viewModel.SelectedPdfFile = selectedFile;
                _contextMenuPdfFile = selectedFile;
            }
            // 選択されていない場合は、マウス位置のアイテムを取得
            else
            {
                var mousePosition = Mouse.GetPosition(listView);
                var hitTestResult = VisualTreeHelper.HitTest(listView, mousePosition);
                if (hitTestResult != null)
                {
                    var item = FindParent<ListViewItem>(hitTestResult.VisualHit);
                    if (item != null && item.DataContext is PdfFileInfo clickedFile)
                    {
                        listView.SelectedItem = clickedFile;
                        if (_viewModel != null)
                        {
                            _viewModel.SelectedPdfFile = clickedFile;
                            _contextMenuPdfFile = clickedFile;
                        }
                    }
                }
            }
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }
}