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
using PdfHandler.Core.Models;
using System.Collections.ObjectModel;

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
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.RootFolder) && _viewModel?.RootFolder != null)
        {
            // TreeViewのItemsSourceを手動で設定
            var items = new ObservableCollection<FolderNode> { _viewModel.RootFolder };
            FolderTreeView.ItemsSource = items;
        }
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (_viewModel != null && e.NewValue is FolderNode node)
        {
            _viewModel.SelectedFolder = node;
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

    // インライン編集機能
    private void FileListView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2 && _viewModel?.SelectedPdfFile != null)
        {
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
            StartInlineEdit(file);
            e.Handled = true;
        }
    }

    private void StartInlineEdit(PdfFileInfo file)
    {
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
}