// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows;
using PdfHandler.Core.Interfaces;

namespace PdfHandler.UI.Views;

/// <summary>
/// PrintDriverSettingsDialog.xaml の相互作用ロジック
/// </summary>
public partial class PrintDriverSettingsDialog : Window
{
    private readonly IPrintToPdfService _printToPdfService;
    private readonly IWorkFolderService _workFolderService;

    public PrintDriverSettingsDialog()
    {
        InitializeComponent();
        
        // DIコンテナから取得
        var app = (App)Application.Current;
        _printToPdfService = app.GetService<IPrintToPdfService>();
        _workFolderService = app.GetService<IWorkFolderService>();

        // 現在の設定を読み込み
        LoadSettings();
    }

    private void LoadSettings()
    {
        var fileNamePattern = _printToPdfService.GetFileNamePattern();
        if (fileNamePattern == IPrintToPdfService.FileNamePattern.Timestamp)
        {
            TimestampRadio.IsChecked = true;
        }
        else
        {
            OriginalFileNameRadio.IsChecked = true;
        }

        WorkFolderTextBox.Text = _workFolderService.GetWorkFolderPath();
    }

    private async void OK_Click(object sender, RoutedEventArgs e)
    {
        // ファイル名パターンを設定
        var fileNamePattern = TimestampRadio.IsChecked == true
            ? IPrintToPdfService.FileNamePattern.Timestamp
            : IPrintToPdfService.FileNamePattern.OriginalFileName;
        
        await _printToPdfService.SetFileNamePatternAsync(fileNamePattern);

        // ワークフォルダを設定
        var workFolderPath = WorkFolderTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(workFolderPath))
        {
            await _workFolderService.SetWorkFolderPathAsync(workFolderPath);
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BrowseWorkFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "ワークフォルダを選択"
        };

        if (dialog.ShowDialog() == true)
        {
            WorkFolderTextBox.Text = dialog.FolderName;
        }
    }
}






