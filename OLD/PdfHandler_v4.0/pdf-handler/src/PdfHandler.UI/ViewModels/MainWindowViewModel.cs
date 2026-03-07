// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.UI.Views;

namespace PdfHandler.UI.ViewModels;

/// <summary>
/// メインウィンドウのViewModel
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IPdfService _pdfService;
    private readonly IPdfMergeService _pdfMergeService;
    private readonly IPdfSplitService _pdfSplitService;
    private readonly IFavoriteService _favoriteService;

    [ObservableProperty]
    private FolderNode? _rootFolder;

    [ObservableProperty]
    private FolderNode? _selectedFolder;

    [ObservableProperty]
    private ObservableCollection<PdfFileInfo> _pdfFiles = new();

    [ObservableProperty]
    private PdfFileInfo? _selectedPdfFile;

    [ObservableProperty]
    private bool _isPreviewVisible = true;

    [ObservableProperty]
    private bool _isThumbnailView = true;

    [ObservableProperty]
    private string _statusText = "準備完了";

    [ObservableProperty]
    private byte[]? _previewImageData;

    [ObservableProperty]
    private int _currentPageNumber = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _zoomPercent = 100;

    // サムネイルサイズ
    [ObservableProperty]
    private int _thumbnailWidth = 120;

    [ObservableProperty]
    private int _thumbnailHeight = 150;

    [ObservableProperty]
    private bool _isSmallThumbnail = false;

    [ObservableProperty]
    private bool _isMediumThumbnail = true;

    [ObservableProperty]
    private bool _isLargeThumbnail = false;

    [ObservableProperty]
    private bool _isExtraLargeThumbnail = false;

    [ObservableProperty]
    private ObservableCollection<FavoriteFolder> _favorites = new();

    public MainWindowViewModel(
        IFileService fileService,
        IPdfService pdfService,
        IPdfMergeService pdfMergeService,
        IPdfSplitService pdfSplitService,
        IFavoriteService favoriteService)
    {
        _fileService = fileService;
        _pdfService = pdfService;
        _pdfMergeService = pdfMergeService;
        _pdfSplitService = pdfSplitService;
        _favoriteService = favoriteService;

        // お気に入りを読み込み
        _ = LoadFavoritesAsync();
    }

    partial void OnSelectedFolderChanged(FolderNode? value)
    {
        if (value != null)
        {
            _ = LoadPdfFilesAsync(value.Path);
        }
    }

    partial void OnSelectedPdfFileChanged(PdfFileInfo? value)
    {
        if (value != null)
        {
            _ = LoadPreviewAsync(value.FilePath);
        }
        else
        {
            PreviewImageData = null;
            CurrentPageNumber = 1;
            TotalPages = 1;
        }
    }

    // お気に入り管理
    private async Task LoadFavoritesAsync()
    {
        var favorites = await _favoriteService.GetFavoritesAsync();
        Favorites.Clear();
        foreach (var fav in favorites.OrderBy(f => f.Name))
        {
            Favorites.Add(fav);
        }
    }

    [RelayCommand]
    private async Task AddFavoriteAsync()
    {
        if (SelectedFolder == null || string.IsNullOrEmpty(SelectedFolder.Path))
        {
            MessageBox.Show("フォルダを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new AddFavoriteDialog(SelectedFolder.Path);
        if (dialog.ShowDialog() == true)
        {
            var success = await _favoriteService.AddFavoriteAsync(dialog.FavoriteName, dialog.FavoritePath);
            if (success)
            {
                await LoadFavoritesAsync();
                StatusText = "お気に入りに追加しました";
            }
            else
            {
                MessageBox.Show("このフォルダは既にお気に入りに追加されています。", "情報",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync(FavoriteFolder favorite)
    {
        if (favorite == null) return;

        var result = MessageBox.Show(
            $"お気に入り '{favorite.Name}' を削除しますか？",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _favoriteService.RemoveFavoriteAsync(favorite.Path);
            await LoadFavoritesAsync();
            StatusText = "お気に入りを削除しました";
        }
    }

    [RelayCommand]
    private async Task OpenFavoriteAsync(FavoriteFolder favorite)
    {
        if (favorite == null || !Directory.Exists(favorite.Path))
        {
            MessageBox.Show("フォルダが見つかりません。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await LoadFolderAsync(favorite.Path);
    }

    // フォルダ操作
    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "PDFファイルが含まれるフォルダを選択してください",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadFolderAsync(dialog.FolderName);
        }
    }

    private async Task LoadFolderAsync(string folderPath)
    {
        StatusText = "フォルダを読み込み中...";

        var folderTree = await _fileService.GetFolderTreeAsync(folderPath);
        if (folderTree != null)
        {
            RootFolder = folderTree;
            
            // 選択されたフォルダを自動的に展開＆選択
            SelectAndExpandFolder(folderTree, folderPath);
            
            StatusText = $"フォルダを開きました: {folderPath}";
        }
        else
        {
            MessageBox.Show("フォルダの読み込みに失敗しました。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText = "フォルダの読み込みに失敗";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedFolder != null)
        {
            await LoadPdfFilesAsync(SelectedFolder.Path);
            StatusText = "更新しました";
        }
    }

    private async Task LoadPdfFilesAsync(string folderPath)
    {
        StatusText = "PDFファイルを読み込み中...";

        var files = await _fileService.GetPdfFilesAsync(folderPath);
        
        PdfFiles.Clear();
        foreach (var file in files)
        {
            // ページ数を取得
            file.PageCount = await _pdfService.GetPageCountAsync(file.FilePath);
            
            // サムネイルを生成
            file.ThumbnailData = await _pdfService.GenerateThumbnailAsync(file.FilePath);
            
            PdfFiles.Add(file);
        }

        StatusText = $"{PdfFiles.Count}個のPDFファイル";
    }

    // プレビュー機能
    private async Task LoadPreviewAsync(string filePath)
    {
        StatusText = "プレビューを読み込み中...";

        TotalPages = await _pdfService.GetPageCountAsync(filePath);
        CurrentPageNumber = 1;
        ZoomPercent = 100;
        
        await LoadPageAsync(filePath, CurrentPageNumber);
        
        StatusText = $"プレビュー表示中: {Path.GetFileName(filePath)}";
    }

    private async Task LoadPageAsync(string filePath, int pageNumber)
    {
        if (pageNumber < 1 || pageNumber > TotalPages) return;

        // DPIをズームパーセントから計算（96 DPI = 100%）
        int dpi = (int)(96 * ZoomPercent / 100.0);
        var imageData = await _pdfService.RenderPageAsync(filePath, pageNumber, dpi);
        PreviewImageData = imageData;
        CurrentPageNumber = pageNumber;
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (SelectedPdfFile != null && CurrentPageNumber > 1)
        {
            await LoadPageAsync(SelectedPdfFile.FilePath, CurrentPageNumber - 1);
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (SelectedPdfFile != null && CurrentPageNumber < TotalPages)
        {
            await LoadPageAsync(SelectedPdfFile.FilePath, CurrentPageNumber + 1);
        }
    }

    [RelayCommand]
    private async Task ZoomInAsync()
    {
        if (ZoomPercent < 200)
        {
            ZoomPercent += 25;
            if (SelectedPdfFile != null)
            {
                await LoadPageAsync(SelectedPdfFile.FilePath, CurrentPageNumber);
            }
        }
    }

    [RelayCommand]
    private async Task ZoomOutAsync()
    {
        if (ZoomPercent > 50)
        {
            ZoomPercent -= 25;
            if (SelectedPdfFile != null)
            {
                await LoadPageAsync(SelectedPdfFile.FilePath, CurrentPageNumber);
            }
        }
    }

    // サムネイルサイズ変更
    [RelayCommand]
    private void SetThumbnailSize(string size)
    {
        IsSmallThumbnail = false;
        IsMediumThumbnail = false;
        IsLargeThumbnail = false;
        IsExtraLargeThumbnail = false;

        switch (size)
        {
            case "Small":
                ThumbnailWidth = 80;
                ThumbnailHeight = 100;
                IsSmallThumbnail = true;
                break;
            case "Medium":
                ThumbnailWidth = 120;
                ThumbnailHeight = 150;
                IsMediumThumbnail = true;
                break;
            case "Large":
                ThumbnailWidth = 180;
                ThumbnailHeight = 225;
                IsLargeThumbnail = true;
                break;
            case "ExtraLarge":
                ThumbnailWidth = 240;
                ThumbnailHeight = 300;
                IsExtraLargeThumbnail = true;
                break;
        }
    }

    // 表示切替
    [RelayCommand]
    private void TogglePreview()
    {
        IsPreviewVisible = !IsPreviewVisible;
    }

    [RelayCommand]
    private void ToggleView()
    {
        IsThumbnailView = !IsThumbnailView;
    }

    // ファイル操作
    [RelayCommand]
    private async Task DeleteFileAsync()
    {
        if (SelectedPdfFile == null) return;

        var result = MessageBox.Show(
            $"ファイル '{Path.GetFileName(SelectedPdfFile.FilePath)}' を削除しますか？\n\nこの操作は元に戻せません。",
            "確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var success = await _fileService.DeleteFileAsync(SelectedPdfFile.FilePath);
            if (success)
            {
                StatusText = "ファイルを削除しました";
                await RefreshAsync();
            }
            else
            {
                MessageBox.Show("ファイルの削除に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "ファイルの削除に失敗";
            }
        }
    }

    // 複数ファイル削除（MainWindow.xaml.csから呼ばれる）
    public async Task DeleteFilesAsync(List<PdfFileInfo> files)
    {
        int successCount = 0;
        int failCount = 0;

        StatusText = $"{files.Count}個のファイルを削除中...";

        foreach (var file in files)
        {
            var success = await _fileService.DeleteFileAsync(file.FilePath);
            if (success)
                successCount++;
            else
                failCount++;
        }

        if (failCount == 0)
        {
            StatusText = $"{successCount}個のファイルを削除しました";
        }
        else
        {
            StatusText = $"{successCount}個削除、{failCount}個失敗";
            MessageBox.Show($"{successCount}個のファイルを削除しました。\n{failCount}個のファイルの削除に失敗しました。",
                "削除結果", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        await RefreshAsync();
    }

    // PDF操作
    [RelayCommand]
    private void MergePdfs()
    {
        // ViewからSelectedItemsを取得する必要があるため、
        // MainWindow.xaml.csでハンドリング
        // このメソッドは使用されない（削除予定）
    }

    // ViewModelから直接呼び出される結合処理
    public async Task MergePdfsWithFilesAsync(List<string> selectedFiles)
    {
        if (selectedFiles.Count < 2)
        {
            MessageBox.Show("結合するには2つ以上のPDFファイルを選択してください。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // デバッグ: 選択されたファイルの順序を出力
        System.Diagnostics.Debug.WriteLine("=== PDF結合: 選択されたファイルの順序 ===");
        for (int i = 0; i < selectedFiles.Count; i++)
        {
            System.Diagnostics.Debug.WriteLine($"  [{i + 1}] {Path.GetFileName(selectedFiles[i])}");
        }
        System.Diagnostics.Debug.WriteLine("=====================================");

        var dialog = new MergePdfDialog(selectedFiles);
        if (dialog.ShowDialog() == true)
        {
            StatusText = "PDFを結合中...";

            var progress = new Progress<int>(percent =>
            {
                StatusText = $"結合中... {percent}%";
            });

            var success = await _pdfMergeService.MergePdfsAsync(dialog.FilePaths, dialog.OutputPath, progress);
            
            if (success)
            {
                MessageBox.Show($"PDFファイルを結合しました。\n保存先: {dialog.OutputPath}", "完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText = "PDF結合完了";
                
                // 結合先フォルダを開いている場合は更新
                string outputDir = Path.GetDirectoryName(dialog.OutputPath) ?? "";
                if (SelectedFolder != null && SelectedFolder.Path == outputDir)
                {
                    await RefreshAsync();
                }
            }
            else
            {
                MessageBox.Show("PDFの結合に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "PDF結合失敗";
            }
        }
    }

    [RelayCommand]
    private async Task SplitPdfAsync()
    {
        if (SelectedPdfFile == null) return;

        var pageCount = await _pdfService.GetPageCountAsync(SelectedPdfFile.FilePath);
        if (pageCount == 0)
        {
            MessageBox.Show("PDFファイルを開けませんでした。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var dialog = new SplitPdfDialog(SelectedPdfFile.FilePath, pageCount);
        if (dialog.ShowDialog() == true)
        {
            StatusText = "PDFを分割中...";

            var progress = new Progress<int>(percent =>
            {
                StatusText = $"分割中... {percent}%";
            });

            bool success = false;

            switch (dialog.SelectedMode)
            {
                case SplitPdfDialog.SplitMode.Range:
                    success = await _pdfSplitService.SplitByRangesAsync(
                        SelectedPdfFile.FilePath,
                        dialog.Ranges,
                        dialog.OutputFolder,
                        dialog.FileNamePattern,
                        progress);
                    break;

                case SplitPdfDialog.SplitMode.Page:
                    success = await _pdfSplitService.SplitByPageAsync(
                        SelectedPdfFile.FilePath,
                        dialog.OutputFolder,
                        dialog.FileNamePattern,
                        progress);
                    break;

                case SplitPdfDialog.SplitMode.Equal:
                    success = await _pdfSplitService.SplitEquallyAsync(
                        SelectedPdfFile.FilePath,
                        dialog.Parts,
                        dialog.OutputFolder,
                        dialog.FileNamePattern,
                        progress);
                    break;
            }

            if (success)
            {
                MessageBox.Show($"PDFファイルを分割しました。\n保存先: {dialog.OutputFolder}", "完了",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText = "PDF分割完了";
                
                // 分割先フォルダを開いている場合は更新
                if (SelectedFolder != null && SelectedFolder.Path == dialog.OutputFolder)
                {
                    await RefreshAsync();
                }
            }
            else
            {
                MessageBox.Show("PDFの分割に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "PDF分割失敗";
            }
        }
    }

    // フォルダを再帰的に検索して選択＆展開
    private void SelectAndExpandFolder(FolderNode node, string targetPath)
    {
        if (node.Path.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
        {
            // 目的のフォルダを見つけた
            node.IsExpanded = true;
            node.IsSelected = true;
            SelectedFolder = node;
            return;
        }

        // targetPathがこのノードの子孫かチェック
        if (targetPath.StartsWith(node.Path, StringComparison.OrdinalIgnoreCase))
        {
            // このノードを展開
            node.IsExpanded = true;
            
            // 子ノードを再帰的に検索
            foreach (var child in node.Children)
            {
                SelectAndExpandFolder(child, targetPath);
            }
        }
    }
}
