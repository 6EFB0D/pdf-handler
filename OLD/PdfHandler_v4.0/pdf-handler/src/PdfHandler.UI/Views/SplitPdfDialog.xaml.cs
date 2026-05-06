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

public partial class SplitPdfDialog : Window
{
    public enum SplitMode
    {
        Range,
        Page,
        Equal
    }

    public SplitMode SelectedMode { get; private set; }
    public string OutputFolder { get; private set; } = string.Empty;
    public string FileNamePattern { get; private set; } = string.Empty;
    public List<(int Start, int End)> Ranges { get; private set; } = new();
    public int Parts { get; private set; }

    private readonly string _sourceFilePath;
    private readonly int _pageCount;

    public SplitPdfDialog(string sourceFilePath, int pageCount)
    {
        InitializeComponent();

        _sourceFilePath = sourceFilePath;
        _pageCount = pageCount;

        TargetFileText.Text = Path.GetFileName(sourceFilePath);
        PageCountText.Text = $"全 {pageCount} ページ";

        // デフォルト値
        var sourceDir = Path.GetDirectoryName(sourceFilePath) ?? "";
        var sourceFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
        
        OutputPathTextBox.Text = sourceDir;
        FileNamePatternTextBox.Text = $"{sourceFileName}_[番号].pdf";
        RangeTextBox.Text = "1-10, 11-20";
        PartsTextBox.Text = "3";
    }

    private void SplitMethod_Changed(object sender, RoutedEventArgs e)
    {
        // ラジオボタンが変更されたときの処理（必要に応じて）
    }

    private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsTextNumeric(e.Text);
    }

    private bool IsTextNumeric(string text)
    {
        return text.All(char.IsDigit);
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

    private void SplitButton_Click(object sender, RoutedEventArgs e)
    {
        var outputDir = OutputPathTextBox.Text.Trim();
        var pattern = FileNamePatternTextBox.Text.Trim();

        // 基本検証
        if (string.IsNullOrWhiteSpace(outputDir) || !Directory.Exists(outputDir))
        {
            MessageBox.Show("有効な保存先フォルダを指定してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(pattern) || !pattern.Contains("[番号]"))
        {
            MessageBox.Show("ファイル名規則に [番号] を含めてください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 分割方法による検証と設定
        if (RangeRadio.IsChecked == true)
        {
            if (!ParseRanges(RangeTextBox.Text.Trim()))
                return;
            SelectedMode = SplitMode.Range;
        }
        else if (PageRadio.IsChecked == true)
        {
            SelectedMode = SplitMode.Page;
        }
        else if (EqualRadio.IsChecked == true)
        {
            if (!int.TryParse(PartsTextBox.Text.Trim(), out int parts) || parts < 2)
            {
                MessageBox.Show("分割数には2以上の整数を指定してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Parts = parts;
            SelectedMode = SplitMode.Equal;
        }

        OutputFolder = outputDir;
        FileNamePattern = pattern;
        DialogResult = true;
    }

    private bool ParseRanges(string rangeText)
    {
        Ranges.Clear();

        if (string.IsNullOrWhiteSpace(rangeText))
        {
            MessageBox.Show("ページ範囲を入力してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        try
        {
            var parts = rangeText.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                var match = Regex.Match(trimmed, @"^(\d+)-(\d+)$");

                if (!match.Success)
                {
                    MessageBox.Show($"無効なページ範囲形式です: {trimmed}\n正しい形式: 1-3, 4-7", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                int start = int.Parse(match.Groups[1].Value);
                int end = int.Parse(match.Groups[2].Value);

                if (start < 1 || start > _pageCount || end < 1 || end > _pageCount || start > end)
                {
                    MessageBox.Show(
                        $"ページ範囲が無効です: {trimmed}\n" +
                        $"1から{_pageCount}の範囲で、開始ページ ≤ 終了ページ である必要があります。",
                        "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                Ranges.Add((start, end));
            }

            return true;
        }
        catch
        {
            MessageBox.Show("ページ範囲の解析に失敗しました。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
