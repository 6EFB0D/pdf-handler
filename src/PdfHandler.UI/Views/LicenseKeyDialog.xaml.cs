// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using PdfHandler.Core.Interfaces;
using PdfHandler.Infrastructure.Configuration;

namespace PdfHandler.UI.Views;

/// <summary>
/// LicenseKeyDialog.xaml の相互作用ロジック
/// </summary>
public partial class LicenseKeyDialog : Window
{
    private readonly ILicenseService _licenseService;
    private readonly AppSettings _appSettings;

    public LicenseKeyDialog()
    {
        InitializeComponent();
        
        var app = (App)Application.Current;
        _licenseService = app.GetService<ILicenseService>();
        _appSettings = app.GetService<AppSettings>();
    }

    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        var licenseKey = LicenseKeyTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            MessageBox.Show("ライセンスキーを入力してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var success = await _licenseService.ActivateLicenseAsync(licenseKey);
        if (success)
        {
            MessageBox.Show("ライセンスのアクティベーションが完了しました。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("ライセンスキーが無効です。正しいキーを入力してください。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ForgotLicenseKey_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        var url = _appSettings.ContactUrl?.Trim();
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"URLを開けませんでした。\n\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}






