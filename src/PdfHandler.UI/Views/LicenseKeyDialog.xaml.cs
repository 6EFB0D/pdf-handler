// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Windows;
using PdfHandler.Core.Interfaces;

namespace PdfHandler.UI.Views;

/// <summary>
/// LicenseKeyDialog.xaml の相互作用ロジック
/// </summary>
public partial class LicenseKeyDialog : Window
{
    private readonly ILicenseService _licenseService;

    public LicenseKeyDialog()
    {
        InitializeComponent();
        
        var app = (App)Application.Current;
        _licenseService = app.GetService<ILicenseService>();
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
            var detail = _licenseService.LastLicenseErrorMessage;
            var message = string.IsNullOrWhiteSpace(detail)
                ? "ライセンスキーが無効です。正しいキーを入力してください。"
                : $"ライセンスキーが無効です。\n\n理由: {detail}";
            MessageBox.Show(message, "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

}






