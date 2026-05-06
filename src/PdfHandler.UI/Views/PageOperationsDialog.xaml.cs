// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace PdfHandler.UI.Views;

public partial class PageOperationsDialog : Window
{
    public enum OperationMode
    {
        Delete,
        Insert
    }

    public OperationMode SelectedMode { get; private set; }
    public IEnumerable<int> PagesToDelete { get; private set; } = Enumerable.Empty<int>();
    public int InsertPosition { get; private set; }
    public string InsertSourcePath { get; private set; } = string.Empty;
    public int InsertSourcePageNumber { get; private set; }
    public string? OutputPath { get; private set; }

    private readonly string _sourceFilePath;
    private readonly int _pageCount;

    public PageOperationsDialog(string sourceFilePath, int pageCount)
    {
        InitializeComponent();

        _sourceFilePath = sourceFilePath;
        _pageCount = pageCount;

        TargetFileText.Text = Path.GetFileName(sourceFilePath);
        PageCountText.Text = $"全 {pageCount} ページ";

        DeletePagesTextBox.Text = "";
        InsertPositionTextBox.Text = "1";
        var sourceDir = Path.GetDirectoryName(sourceFilePath) ?? "";
        var sourceFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        OutputPathTextBox.Text = Path.Combine(sourceDir, $"{sourceFileName}_edited.pdf");
    }

    private void OperationMode_Changed(object sender, RoutedEventArgs e)
    {
        if (DeletePanel != null)
            DeletePanel.IsEnabled = DeleteRadio?.IsChecked == true;
        if (InsertPanel != null)
            InsertPanel.IsEnabled = InsertRadio?.IsChecked == true;
    }

    private void OutputMode_Changed(object sender, RoutedEventArgs e)
    {
        if (OutputPathGrid != null && NewFileRadio != null)
            OutputPathGrid.Visibility = NewFileRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void BrowseInsertSource_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PDFファイル (*.pdf)|*.pdf",
            Title = "挿入するページの元PDFを選択"
        };
        if (dialog.ShowDialog() == true)
        {
            InsertSourcePathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDFファイル (*.pdf)|*.pdf",
            Title = "出力先を指定",
            FileName = Path.GetFileName(OutputPathTextBox.Text)
        };
        if (!string.IsNullOrEmpty(OutputPathTextBox.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(OutputPathTextBox.Text);
        }
        if (dialog.ShowDialog() == true)
        {
            OutputPathTextBox.Text = dialog.FileName;
        }
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DeleteRadio.IsChecked == true)
        {
            if (!ParseDeletePages(out var pages))
                return;
            PagesToDelete = pages;
            SelectedMode = OperationMode.Delete;
        }
        else
        {
            if (!ParseInsertSettings(out var position, out var sourcePath, out var sourcePage))
                return;
            InsertPosition = position;
            InsertSourcePath = sourcePath;
            InsertSourcePageNumber = sourcePage;
            SelectedMode = OperationMode.Insert;
        }

        if (OverwriteRadio.IsChecked == true)
        {
            OutputPath = null;
        }
        else
        {
            var path = OutputPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show("出力先ファイルを指定してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                MessageBox.Show("出力先のフォルダが存在しません。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OutputPath = path;
        }

        DialogResult = true;
        Close();
    }

    private bool ParseDeletePages(out List<int> pages)
    {
        pages = new List<int>();
        var text = DeletePagesTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("削除するページを指定してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        try
        {
            foreach (var part in text.Split(','))
            {
                var trimmed = part.Trim();
                var rangeMatch = Regex.Match(trimmed, @"^(\d+)-(\d+)$");
                var singleMatch = Regex.Match(trimmed, @"^(\d+)$");

                if (rangeMatch.Success)
                {
                    int start = int.Parse(rangeMatch.Groups[1].Value);
                    int end = int.Parse(rangeMatch.Groups[2].Value);
                    if (start < 1 || end > _pageCount || start > end)
                    {
                        MessageBox.Show($"無効なページ範囲: {trimmed}\n1〜{_pageCount}の範囲で指定してください。", "エラー",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    for (int i = start; i <= end; i++)
                        pages.Add(i);
                }
                else if (singleMatch.Success)
                {
                    int p = int.Parse(singleMatch.Groups[1].Value);
                    if (p < 1 || p > _pageCount)
                    {
                        MessageBox.Show($"無効なページ番号: {p}\n1〜{_pageCount}の範囲で指定してください。", "エラー",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    pages.Add(p);
                }
                else
                {
                    MessageBox.Show($"無効な形式: {trimmed}\n例: 1, 3, 5-7", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            if (pages.Count == 0)
            {
                MessageBox.Show("削除するページを指定してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (pages.Count >= _pageCount)
            {
                MessageBox.Show("全てのページを削除することはできません。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            pages = pages.Distinct().OrderBy(x => x).ToList();
            return true;
        }
        catch
        {
            MessageBox.Show("ページ指定の解析に失敗しました。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private bool ParseInsertSettings(out int position, out string sourcePath, out int sourcePage)
    {
        position = 0;
        sourcePath = string.Empty;
        sourcePage = 0;

        if (!int.TryParse(InsertPositionTextBox.Text.Trim(), out position) || position < 1 || position > _pageCount + 1)
        {
            MessageBox.Show($"挿入位置は1〜{_pageCount + 1}の範囲で指定してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        sourcePath = InsertSourcePathTextBox.Text.Trim();
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            MessageBox.Show("挿入元のPDFファイルを選択してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!int.TryParse(InsertSourcePageTextBox.Text.Trim(), out sourcePage) || sourcePage < 1)
        {
            MessageBox.Show("挿入元のページ番号を正しく入力してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
