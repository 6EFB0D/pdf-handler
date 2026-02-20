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
        
        // Premiumプランの表示/非表示を制御
        if (PremiumPlanBorder != null)
        {
            PremiumPlanBorder.Visibility = _appSettings.EnablePremiumPlan 
                ? System.Windows.Visibility.Visible 
                : System.Windows.Visibility.Collapsed;
        }

        // ヘッダーメッセージを動的に設定
        UpdateHeaderMessage();
    }
    
    private void UpdateHeaderMessage()
    {
        if (HeaderMessageText == null) return;

        var licenseInfo = _licenseService.GetLicenseInfo();
        var isPurchased = licenseInfo.Plan == LicensePlan.StandardPurchased;
        var isSubscribed = licenseInfo.Plan == LicensePlan.StandardSubscription || licenseInfo.Plan == LicensePlan.Premium || licenseInfo.Plan == LicensePlan.PremiumBYOK;

        if (isPurchased)
        {
            HeaderMessageText.Text = "ご購入いただきありがとうございます。サブスクリプションに切り替えると、自動バージョンアップ・メールサポートなどのメリットをご利用いただけます。";
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

        // サブスクリプション契約済みの場合は契約済みパネルを表示
        if (SubscriptionContractStatePanel != null && SubscriptionNotContractStatePanel != null)
        {
            if (isSubscribed)
            {
                SubscriptionContractStatePanel.Visibility = Visibility.Visible;
                SubscriptionNotContractStatePanel.Visibility = Visibility.Collapsed;
                if (AdditionalSubscriptionCheckBox != null) AdditionalSubscriptionCheckBox.IsChecked = false;
                if (StartSubscriptionButton != null) StartSubscriptionButton.IsEnabled = false;
            }
            else
            {
                SubscriptionContractStatePanel.Visibility = Visibility.Collapsed;
                SubscriptionNotContractStatePanel.Visibility = Visibility.Visible;
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

    private void AdditionalSubscriptionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (StartSubscriptionButton != null && AdditionalSubscriptionCheckBox != null)
        {
            StartSubscriptionButton.IsEnabled = AdditionalSubscriptionCheckBox.IsChecked == true;
        }
    }

    private async void PurchaseStandardLifetime_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DebugLogger.WriteLine("=== 買い切り版購入開始 ===");
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                LicensePlan.StandardPurchased, 
                isSubscription: false);
            
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                throw new Exception("Checkout URLが取得できませんでした。");
            }
            
            DebugLogger.WriteLine($"ブラウザを起動します... (URL: {checkoutUrl})");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = checkoutUrl,
                    UseShellExecute = true
                });
                DebugLogger.WriteLine("ブラウザ起動成功");
            }
            catch (Exception procEx)
            {
                DebugLogger.WriteLine($"ブラウザ起動エラー: {procEx.GetType().Name} - {procEx.Message}");
                throw;
            }
            
            MessageBox.Show(
                "ブラウザで決済ページが開きました。\n\n決済完了後、ライセンスキーがメールで送信されます。",
                "決済ページを開きました",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            DebugLogger.WriteLine($"買い切り版購入エラー: {ex.GetType().Name} - {ex.Message}");
            DebugLogger.WriteLine($"スタックトレース: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                DebugLogger.WriteLine($"内部例外: {ex.InnerException.Message}");
            }
            
            var logPath = DebugLogger.GetLogFilePath();
            MessageBox.Show(
                $"決済ページの表示に失敗しました。\n\nエラー: {ex.Message}\n\n詳細はログファイルを確認してください。\nログファイル: {logPath}\n\nライセンスキーを直接入力する方法もご利用いただけます。",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void StartTrialStandard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DebugLogger.WriteLine("=== サブスクリプション開始 ===");
            DebugLogger.WriteLine($"Supabase URL: {_appSettings.Supabase.Url}");
            DebugLogger.WriteLine($"AnonKey設定: {!string.IsNullOrEmpty(_appSettings.Supabase.AnonKey)}");
            
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                LicensePlan.StandardSubscription, 
                isSubscription: true);
            
            DebugLogger.WriteLine($"Checkout URL取得: {!string.IsNullOrWhiteSpace(checkoutUrl)}");
            DebugLogger.WriteLine($"Checkout URL: {checkoutUrl}");
            
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                throw new Exception("Checkout URLが取得できませんでした。");
            }
            
            DebugLogger.WriteLine("ブラウザを起動します...");
            DebugLogger.WriteLine($"Process.Start呼び出し: FileName={checkoutUrl}, UseShellExecute=true");
            
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = checkoutUrl,
                    UseShellExecute = true
                });
                DebugLogger.WriteLine("ブラウザ起動成功");
            }
            catch (Exception procEx)
            {
                DebugLogger.WriteLine($"ブラウザ起動エラー: {procEx.GetType().Name} - {procEx.Message}");
                throw;
            }
            
            MessageBox.Show(
                "ブラウザで決済ページが開きました。\n\n決済完了後、ライセンスキーがメールで送信されます。",
                "サブスクリプション開始",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            DebugLogger.WriteLine($"エラー詳細: {ex.GetType().Name}");
            DebugLogger.WriteLine($"エラーメッセージ: {ex.Message}");
            DebugLogger.WriteLine($"スタックトレース: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                DebugLogger.WriteLine($"内部例外: {ex.InnerException.Message}");
            }
            
            var logPath = DebugLogger.GetLogFilePath();
            MessageBox.Show(
                $"決済ページの表示に失敗しました。\n\nエラー: {ex.Message}\n\n詳細はログファイルを確認してください。\nログファイル: {logPath}\n\nライセンスキーを直接入力する方法もご利用いただけます。",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void StartTrialPremium_Click(object sender, RoutedEventArgs e)
    {
        // Premium版が非公開の場合は実行しない
        if (!_appSettings.EnablePremiumPlan)
        {
            MessageBox.Show(
                "Premiumプランは現在公開されていません。",
                "情報",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                LicensePlan.Premium, 
                isSubscription: true);
            
            if (string.IsNullOrWhiteSpace(checkoutUrl))
            {
                throw new Exception("Checkout URLが取得できませんでした。");
            }
            
            Process.Start(new ProcessStartInfo
            {
                FileName = checkoutUrl,
                UseShellExecute = true
            });
            
            MessageBox.Show(
                "ブラウザで決済ページが開きました。\n\n決済完了後、ライセンスキーがメールで送信されます。",
                "サブスクリプション開始",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"決済ページの表示に失敗しました。\n\nエラー: {ex.Message}\n\nライセンスキーを直接入力する方法もご利用いただけます。",
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




