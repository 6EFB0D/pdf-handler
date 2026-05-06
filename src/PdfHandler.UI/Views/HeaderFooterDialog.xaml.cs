// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.UI;

namespace PdfHandler.UI.Views;

public partial class HeaderFooterDialog : Window
{
    public HeaderFooterSettings? AppliedSettings { get; private set; }

    private readonly string _sourceFilePath;
    private readonly int _pageCount;
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PdfHandler", "headerfooter.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public HeaderFooterDialog(string sourceFilePath, int pageCount)
    {
        InitializeComponent();

        _sourceFilePath = sourceFilePath;
        _pageCount = pageCount;

        TargetFileText.Text = Path.GetFileName(sourceFilePath);
        PageCountText.Text = $"全 {pageCount} ページ";

        Loaded += HeaderFooterDialog_Loaded;

        var fontItems = new[] { "MS Gothic", "Meiryo", "Yu Gothic", "Yu Gothic UI", "Meiryo UI", "MS Mincho", "Arial", "Times New Roman" };
        foreach (var f in fontItems)
        {
            HeaderFontFamilyComboBox.Items.Add(f);
            FooterFontFamilyComboBox.Items.Add(f);
        }
    }

    private void HeaderFooterDialog_Loaded(object sender, RoutedEventArgs e)
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<HeaderFooterSettings>(json);
            if (settings == null) return;

            ShowHeaderCheckBox.IsChecked = settings.ShowHeader;
            DocumentTitleTextBox.Text = settings.DocumentTitle;
            HeaderLeftRadio.IsChecked = settings.HeaderAlignment == HeaderFooterAlignment.Left;
            HeaderCenterRadio.IsChecked = settings.HeaderAlignment == HeaderFooterAlignment.Center;
            HeaderRightRadio.IsChecked = settings.HeaderAlignment == HeaderFooterAlignment.Right;
            HeaderMarginTextBox.Text = settings.HeaderMarginPt.ToString();

            ShowFooterCheckBox.IsChecked = settings.ShowFooter;
            PageFormatSingleRadio.IsChecked = settings.PageNumberFormat == PageNumberFormat.Single;
            PageFormatFractionRadio.IsChecked = settings.PageNumberFormat == PageNumberFormat.Fraction;
            PageFormatHyphenRadio.IsChecked = settings.PageNumberFormat == PageNumberFormat.Hyphen;
            FooterLeftRadio.IsChecked = settings.FooterAlignment == HeaderFooterAlignment.Left;
            FooterCenterRadio.IsChecked = settings.FooterAlignment == HeaderFooterAlignment.Center;
            FooterRightRadio.IsChecked = settings.FooterAlignment == HeaderFooterAlignment.Right;
            FooterMarginTextBox.Text = settings.FooterMarginPt.ToString();

            var hf = string.IsNullOrEmpty(settings.HeaderFontFamily) ? settings.FontFamily : settings.HeaderFontFamily;
            var hs = settings.HeaderFontSize > 0 ? settings.HeaderFontSize : settings.FontSize;
            HeaderFontFamilyComboBox.Text = hf;
            HeaderFontSizeTextBox.Text = hs.ToString();

            var ff = string.IsNullOrEmpty(settings.FooterFontFamily) ? settings.FontFamily : settings.FooterFontFamily;
            var fs = settings.FooterFontSize > 0 ? settings.FooterFontSize : settings.FontSize;
            FooterFontFamilyComboBox.Text = ff;
            FooterFontSizeTextBox.Text = fs.ToString();

            AutoReapplyCheckBox.IsChecked = settings.AutoReapplyOnPageEdit;
        }
        catch { /* ignore */ }
    }

    private void SaveSettings(HeaderFooterSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* ignore */ }
    }

    private HeaderFooterSettings BuildSettings()
    {
        return new HeaderFooterSettings
        {
            ShowHeader = ShowHeaderCheckBox.IsChecked == true,
            DocumentTitle = DocumentTitleTextBox.Text.Trim(),
            HeaderAlignment = HeaderLeftRadio.IsChecked == true ? HeaderFooterAlignment.Left :
                HeaderRightRadio.IsChecked == true ? HeaderFooterAlignment.Right : HeaderFooterAlignment.Center,
            HeaderMarginPt = double.TryParse(HeaderMarginTextBox.Text, out var hm) ? Math.Clamp(hm, 0, 72) : 12,

            ShowFooter = ShowFooterCheckBox.IsChecked == true,
            PageNumberFormat = PageFormatSingleRadio.IsChecked == true ? PageNumberFormat.Single :
                PageFormatHyphenRadio.IsChecked == true ? PageNumberFormat.Hyphen : PageNumberFormat.Fraction,
            FooterAlignment = FooterLeftRadio.IsChecked == true ? HeaderFooterAlignment.Left :
                FooterRightRadio.IsChecked == true ? HeaderFooterAlignment.Right : HeaderFooterAlignment.Center,
            FooterMarginPt = double.TryParse(FooterMarginTextBox.Text, out var fm) ? Math.Clamp(fm, 0, 72) : 12,

            HeaderFontFamily = string.IsNullOrWhiteSpace(HeaderFontFamilyComboBox.Text) ? "MS Gothic" : HeaderFontFamilyComboBox.Text.Trim(),
            HeaderFontSize = double.TryParse(HeaderFontSizeTextBox.Text, out var hs) ? Math.Clamp(hs, 6, 24) : 9,
            FooterFontFamily = string.IsNullOrWhiteSpace(FooterFontFamilyComboBox.Text) ? "MS Gothic" : FooterFontFamilyComboBox.Text.Trim(),
            FooterFontSize = double.TryParse(FooterFontSizeTextBox.Text, out var fs) ? Math.Clamp(fs, 6, 24) : 9,
            FontFamily = "MS Gothic",
            FontSize = 9,
            AutoReapplyOnPageEdit = AutoReapplyCheckBox.IsChecked == true
        };
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = BuildSettings();
        if (settings.ShowHeader && string.IsNullOrWhiteSpace(settings.DocumentTitle))
        {
            MessageBox.Show("ヘッダーを表示する場合、文書タイトルを入力してください。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!settings.ShowHeader && !settings.ShowFooter)
        {
            MessageBox.Show("ヘッダーまたはフッターのいずれかを有効にしてください。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var app = (App)Application.Current;
        var service = app.GetService<IHeaderFooterService>();

        ApplyButton.IsEnabled = false;
        try
        {
            var progress = new Progress<int>(p => { /* 必要ならステータス表示 */ });
            var success = await service.AddHeaderFooterAsync(_sourceFilePath, settings, null, progress);
            if (success)
            {
                AppliedSettings = settings;
                SaveSettings(settings);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("ヘッダ・フッターの適用に失敗しました。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            ApplyButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
