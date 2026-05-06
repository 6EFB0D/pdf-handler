// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace PdfHandler.UI.Views;

public partial class MergePdfDialog : Window
{
    public class FileItem
    {
        public int Index { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    private ObservableCollection<FileItem> _files = new();
    public string OutputPath { get; private set; } = string.Empty;
    public List<string> FilePaths => _files.Select(f => f.FilePath).ToList();

    public MergePdfDialog(List<string> filePaths)
    {
        InitializeComponent();

        // ファイルリストを設定
        for (int i = 0; i < filePaths.Count; i++)
        {
            _files.Add(new FileItem
            {
                Index = i + 1,
                FileName = Path.GetFileName(filePaths[i]),
                FilePath = filePaths[i]
            });
        }

        FileListBox.ItemsSource = _files;

        // デフォルトの保存先とファイル名
        if (filePaths.Count > 0)
        {
            var firstFileDir = Path.GetDirectoryName(filePaths[0]) ?? "";
            OutputPathTextBox.Text = firstFileDir;
            FileNameTextBox.Text = "merged.pdf";
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedIndex = FileListBox.SelectedIndex;
        if (selectedIndex <= 0) return;

        var item = _files[selectedIndex];
        _files.RemoveAt(selectedIndex);
        _files.Insert(selectedIndex - 1, item);

        // インデックスを更新
        UpdateIndices();

        FileListBox.SelectedIndex = selectedIndex - 1;
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedIndex = FileListBox.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _files.Count - 1) return;

        var item = _files[selectedIndex];
        _files.RemoveAt(selectedIndex);
        _files.Insert(selectedIndex + 1, item);

        // インデックスを更新
        UpdateIndices();

        FileListBox.SelectedIndex = selectedIndex + 1;
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedIndex = FileListBox.SelectedIndex;
        if (selectedIndex < 0) return;

        _files.RemoveAt(selectedIndex);
        UpdateIndices();

        if (_files.Count == 0)
        {
            MessageBox.Show("結合するファイルが1つもありません。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateIndices()
    {
        for (int i = 0; i < _files.Count; i++)
        {
            _files[i].Index = i + 1;
        }
        FileListBox.Items.Refresh();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "保存先フォルダを選択してください",
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrEmpty(OutputPathTextBox.Text))
        {
            dialog.SelectedPath = OutputPathTextBox.Text;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void MergeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count < 2)
        {
            MessageBox.Show("結合するには2つ以上のファイルが必要です。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var outputDir = OutputPathTextBox.Text.Trim();
        var fileName = FileNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(outputDir) || !Directory.Exists(outputDir))
        {
            MessageBox.Show("有効な保存先フォルダを指定してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            MessageBox.Show("ファイル名を入力してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".pdf";
        }

        OutputPath = Path.Combine(outputDir, fileName);

        // 上書き確認
        if (File.Exists(OutputPath))
        {
            var result = MessageBox.Show(
                $"ファイル '{fileName}' は既に存在します。上書きしますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
