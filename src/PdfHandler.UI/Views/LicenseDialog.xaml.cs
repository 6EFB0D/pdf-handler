// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.Infrastructure.Configuration;
using PdfHandler.Infrastructure.Helpers;

namespace PdfHandler.UI.Views;

/// <summary>
/// ライセンス統合ダイアログ（状態・購入・デバイス管理を一元化）
/// </summary>
public partial class LicenseDialog : Window
{
    private readonly IPaymentService _paymentService;
    private readonly AppSettings _appSettings;
    private readonly ILicenseService _licenseService;

    public LicenseDialog()
    {
        InitializeComponent();
        var app = (App)Application.Current;
        _paymentService = app.GetService<IPaymentService>();
        _appSettings = app.GetService<AppSettings>();
        _licenseService = app.GetService<ILicenseService>();

        LoadLicenseInfo();
    }

    private void LoadLicenseInfo()
    {
        try
        {
            var licenseInfo = _licenseService.GetLicenseInfo();
            var isPurchased = licenseInfo.Plan == LicensePlan.StandardPurchased;

            LicensePlanText.Text = licenseInfo.Plan switch
            {
                LicensePlan.Trial => "試用期間中",
                LicensePlan.StandardPurchased => "Standard版（買い切り）",
                _ => "Standard版（買い切り）",
            };

            LicenseStatusText.ClearValue(System.Windows.Controls.TextBlock.ForegroundProperty);
            if (_licenseService.IsTrialValid())
            {
                var remainingDays = _licenseService.GetRemainingTrialDays();
                LicenseStatusText.Text = $"試用期間: 残り{remainingDays}日";
                TrialDaysText.Text = $"試用期間終了日: {licenseInfo.FirstLaunchDate.AddDays(14):yyyy年MM月dd日}";
                TrialDaysText.Visibility = Visibility.Visible;
                DeviceManagerButton.Visibility = Visibility.Collapsed;
            }
            else if (_licenseService.IsLicenseValid())
            {
                LicenseStatusText.Text = "ライセンス有効";
                TrialDaysText.Visibility = Visibility.Collapsed;
                DeviceManagerButton.Visibility = Visibility.Visible;
            }
            else
            {
                LicenseStatusText.Text = "ライセンス無効";
                LicenseStatusText.Foreground = System.Windows.Media.Brushes.Red;
                TrialDaysText.Visibility = Visibility.Collapsed;
                DeviceManagerButton.Visibility = Visibility.Collapsed;
            }

            UpdateHeaderMessage();
            UpdatePurchasePanels(isPurchased);
        }
        catch (Exception ex)
        {
            LicensePlanText.Text = "エラー";
            LicenseStatusText.Text = $"読み込みに失敗しました: {ex.Message}";
            LicenseStatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void UpdateHeaderMessage()
    {
        if (HeaderMessageText == null) return;
        var licenseInfo = _licenseService.GetLicenseInfo();

        HeaderMessageText.Text = licenseInfo.Plan == LicensePlan.StandardPurchased
            ? "ご購入いただきありがとうございます。"
            : _licenseService.IsTrialValid()
                ? $"試用期間中です（残り{_licenseService.GetRemainingTrialDays()}日）。ライセンスを購入して全機能をご利用ください。"
                : !_licenseService.IsLicenseValid()
                    ? "試用期間が終了しました。ライセンスを購入して続けてご利用ください。"
                    : "ライセンスを購入して全機能をご利用ください。";
    }

    private void UpdatePurchasePanels(bool isPurchased)
    {
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
            PurchaseStandardButton.IsEnabled = AdditionalPurchaseCheckBox.IsChecked == true;
    }

    private void DeviceManagerButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LicenseManagerDialog { Owner = this };
        dialog.ShowDialog();
        LoadLicenseInfo();
    }

    private async void PurchaseStandardLifetime_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DebugLogger.WriteLine("=== 買い切り版購入開始 ===");
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

            DebugLogger.WriteLine($"購入手続きメール送信成功: {result.EmailMasked}");

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
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            DebugLogger.WriteLine($"買い切り版購入エラー: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null) DebugLogger.WriteLine($"内部例外: {ex.InnerException.Message}");

            MessageBox.Show(
                $"{ex.Message}\n\n" +
                "問題が解消しない場合は support@office-goplan.com までお問い合わせください。\n" +
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
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show($"URLを開けませんでした。\n\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void EnterLicenseKey_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LicenseKeyDialog { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            LoadLicenseInfo();
        }
    }

    private void TermsOfUse_Click(object sender, RoutedEventArgs e) => ShowLegalDocument("TERMS_OF_USE.txt", "利用規約", "https://github.com/6EFB0D/pdf-handler/blob/main/TERMS_OF_USE.txt");
    private void PrivacyPolicy_Click(object sender, RoutedEventArgs e) => ShowLegalDocument("PRIVACY_POLICY.txt", "プライバシーポリシー", "https://github.com/6EFB0D/pdf-handler/blob/main/PRIVACY_POLICY.txt");

    private void ShowLegalDocument(string filename, string title, string onlineUrl)
    {
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Legal", filename);
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                new LegalDocumentViewer(title, content) { Owner = this }.ShowDialog();
            }
            else if (MessageBox.Show($"{title}のファイルが見つかりませんでした。\n\nオンライン版を表示しますか？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo { FileName = onlineUrl, UseShellExecute = true });
            }
        }
        catch (Exception ex) { MessageBox.Show($"{title}の表示中にエラーが発生しました。\n\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
