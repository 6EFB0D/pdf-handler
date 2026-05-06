// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Windows;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.Infrastructure.Configuration;
using PdfHandler.Infrastructure.Helpers;

namespace PdfHandler.UI.Views;

/// <summary>
/// PurchaseDialog.xaml の相互作用ロジック
/// </summary>
public partial class PurchaseDialog : Window
{
    private readonly IPaymentService _paymentService;
    private readonly AppSettings _appSettings;
    private readonly ILicenseService _licenseService;

    public PurchaseDialog()
    {
        InitializeComponent();
        
        // DIコンテナから取得
        var app = (App)Application.Current;
        _paymentService = app.GetService<IPaymentService>();
        _appSettings = app.GetService<AppSettings>();
        _licenseService = app.GetService<ILicenseService>();

        // ヘッダーメッセージを動的に設定
        UpdateHeaderMessage();
    }
    
    private void UpdateHeaderMessage()
    {
        if (HeaderMessageText == null) return;

        var licenseInfo = _licenseService.GetLicenseInfo();
        var isPurchased = licenseInfo.Plan == LicensePlan.StandardPurchased;

        if (isPurchased)
        {
            HeaderMessageText.Text = "ご購入いただきありがとうございます。";
        }
        else if (_licenseService.IsTrialValid())
        {
            var remainingDays = _licenseService.GetRemainingTrialDays();
            HeaderMessageText.Text = $"試用期間中です（残り{remainingDays}日）。ライセンスを購入して全機能をご利用ください。";
        }
        else if (!_licenseService.IsLicenseValid())
        {
            HeaderMessageText.Text = "試用期間が終了しました。ライセンスを購入して続けてご利用ください。";
        }
        else
        {
            HeaderMessageText.Text = "ライセンスを購入して全機能をご利用ください。";
        }

        // 買い切り版購入済みの場合は購入済みパネルを表示、未購入時は購入ボタンを表示
        if (PurchasedStatePanel != null && NotPurchasedStatePanel != null)
        {
            if (isPurchased)
            {
                PurchasedStatePanel.Visibility = Visibility.Visible;
                NotPurchasedStatePanel.Visibility = Visibility.Collapsed;
                if (AdditionalPurchaseCheckBox != null) AdditionalPurchaseCheckBox.IsChecked = false;
                if (PurchaseStandardButton != null) PurchaseStandardButton.IsEnabled = false;
            }
            else
            {
                PurchasedStatePanel.Visibility = Visibility.Collapsed;
                NotPurchasedStatePanel.Visibility = Visibility.Visible;
            }
        }
    }

    private void AdditionalPurchaseCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (PurchaseStandardButton != null && AdditionalPurchaseCheckBox != null)
        {
            PurchaseStandardButton.IsEnabled = AdditionalPurchaseCheckBox.IsChecked == true;
        }
    }

    private async void PurchaseStandardLifetime_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DebugLogger.LogInfo("=== 買い切り版購入開始 ===");
            var emailDlg = new PurchaseEmailDialog { Owner = this };
            if (emailDlg.ShowDialog() != true) return;

            var result = await _paymentService.RequestCheckoutAsync(
                LicensePlan.StandardPurchased, emailDlg.CustomerEmail);

            if (!result.Success)
            {
                throw new Exception(string.IsNullOrWhiteSpace(result.Message)
                    ? "購入手続きメールの送信に失敗しました。"
                    : result.Message);
            }

            DebugLogger.LogInfo($"購入手続きメール送信成功: {result.EmailMasked}");

            MessageBox.Show(
                $"{result.EmailMasked} 宛にお支払い手続きのメールをお送りしました。\n\n" +
                "メールに記載のリンクから決済にお進みください。\n" +
                "リンクの有効期限は送信から 24 時間です。\n\n" +
                "※ メールが届かない場合:\n" +
                "・迷惑メールフォルダをご確認ください（最大5分ほどかかる場合があります）\n" +
                "・メールアドレスが正しく入力されているかご確認ください\n" +
                "・届かない場合はもう一度お試しください",
                "お支払い手続きメールを送信しました",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // メール送信後はダイアログを閉じて、ユーザーは受信箱で作業継続
            DialogResult = false;
            Close();
        }
        catch (InvalidOperationException ex)
        {
            // 入力バリデーションエラー（メール未入力・形式不正など）
            MessageBox.Show(
                ex.Message,
                "入力エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(ErrorCodes.PurchaseStartFailed, "買い切り版購入エラー", ex);
            MessageBox.Show(
                ErrorCodes.UserMessage(ErrorCodes.PurchaseStartFailed) + "\n\n" +
                "既にお持ちのライセンスキーは「ライセンスキーを入力」からご登録いただけます。",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void VolumeLicenseContact_Click(object sender, RoutedEventArgs e)
    {
        var url = _appSettings.ContactUrl?.Trim();
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"URLを開けませんでした。\n\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void EnterLicenseKey_Click(object sender, RoutedEventArgs e)
    {
        var licenseKeyDialog = new LicenseKeyDialog();
        if (licenseKeyDialog.ShowDialog() == true)
        {
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}




