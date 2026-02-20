// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;

namespace PdfHandler.UI.Views
{
    /// <summary>
    /// LicenseInfoDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class LicenseInfoDialog : Window
    {
        private readonly ILicenseService _licenseService;

        public LicenseInfoDialog()
        {
            InitializeComponent();
            
            // DIコンテナから取得
            var app = (App)Application.Current;
            _licenseService = app.GetService<ILicenseService>();
            
            // ライセンス情報を表示
            LoadLicenseInfo();
        }

        private void LoadLicenseInfo()
        {
            try
            {
                var licenseInfo = _licenseService.GetLicenseInfo();
                
                // プラン情報を表示
                LicensePlanText.Text = licenseInfo.Plan switch
                {
                    LicensePlan.Trial => "試用期間中",
                    LicensePlan.StandardPurchased => "Standard版（買い切り）",
                    LicensePlan.StandardSubscription => "Standard版（サブスクリプション）",
                    LicensePlan.Premium => "Premium版",
                    LicensePlan.PremiumBYOK => "Premium版（BYOK）",
                    _ => "ライセンス情報不明"
                };
                
                // ライセンス状態を表示
                if (_licenseService.IsTrialValid())
                {
                    var remainingDays = _licenseService.GetRemainingTrialDays();
                    LicenseStatusText.Text = $"試用期間: 残り{remainingDays}日";
                    TrialDaysText.Text = $"試用期間終了日: {licenseInfo.FirstLaunchDate.AddDays(14):yyyy年MM月dd日}";
                    TrialDaysText.Visibility = Visibility.Visible;
                    // 試用期間中は購入ボタンを表示
                    PurchaseButton.Visibility = Visibility.Visible;
                    LicenseManagerButton.Visibility = Visibility.Collapsed;
                }
                else if (_licenseService.IsLicenseValid())
                {
                    LicenseStatusText.Text = "ライセンス有効";
                    
                    if (licenseInfo.Plan == LicensePlan.StandardSubscription || 
                        licenseInfo.Plan == LicensePlan.Premium || 
                        licenseInfo.Plan == LicensePlan.PremiumBYOK)
                    {
                        if (licenseInfo.SubscriptionRenewalDate.HasValue)
                        {
                            SubscriptionRenewalText.Text = $"サブスクリプション更新日: {licenseInfo.SubscriptionRenewalDate.Value:yyyy年MM月dd日}";
                            SubscriptionRenewalText.Visibility = Visibility.Visible;
                        }
                    }
                    
                    if (licenseInfo.LastVerificationDate.HasValue)
                    {
                        LastVerificationText.Text = $"最終検証日時: {licenseInfo.LastVerificationDate.Value:yyyy年MM月dd日 HH:mm}";
                        LastVerificationText.Visibility = Visibility.Visible;
                    }
                    
                    if (licenseInfo.NextVerificationDate.HasValue)
                    {
                        NextVerificationText.Text = $"次回検証日時: {licenseInfo.NextVerificationDate.Value:yyyy年MM月dd日 HH:mm}";
                        NextVerificationText.Visibility = Visibility.Visible;
                    }
                    // 有効なライセンスがある場合は購入ボタンを非表示、ライセンス管理を表示
                    PurchaseButton.Visibility = Visibility.Collapsed;
                    LicenseManagerButton.Visibility = Visibility.Visible;
                }
                else
                {
                    LicenseStatusText.Text = "ライセンス無効";
                    LicenseStatusText.Foreground = System.Windows.Media.Brushes.Red;
                    // ライセンス無効の場合は購入ボタンを表示
                    PurchaseButton.Visibility = Visibility.Visible;
                    LicenseManagerButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LicensePlanText.Text = "エラー";
                LicenseStatusText.Text = $"ライセンス情報の読み込みに失敗しました: {ex.Message}";
                LicenseStatusText.Foreground = System.Windows.Media.Brushes.Red;
                PurchaseButton.Visibility = Visibility.Collapsed;
                LicenseManagerButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 利用規約
        /// </summary>
        private void TermsOfUse_Click(object sender, RoutedEventArgs e)
        {
            ShowLegalDocument("TERMS_OF_USE.txt", "利用規約",
                "https://github.com/6EFB0D/pdf-handler/blob/main/TERMS_OF_USE.txt");
        }

        /// <summary>
        /// プライバシーポリシー
        /// </summary>
        private void PrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            ShowLegalDocument("PRIVACY_POLICY.txt", "プライバシーポリシー",
                "https://github.com/6EFB0D/pdf-handler/blob/main/PRIVACY_POLICY.txt");
        }

        /// <summary>
        /// オープンソースライセンス（すべて）
        /// </summary>
        private void OpenSourceLicenses_Click(object sender, RoutedEventArgs e)
        {
            ShowLegalDocument("OPEN_SOURCE_LICENSES.txt", "オープンソースライセンス",
                "https://github.com/6EFB0D/pdf-handler/blob/main/OPEN_SOURCE_LICENSES.txt");
        }

        /// <summary>
        /// PdfSharpライセンス（OPEN_SOURCE_LICENSES.txtを開く）
        /// </summary>
        private void PdfSharpLicense_Click(object sender, RoutedEventArgs e)
        {
            ShowLegalDocument("OPEN_SOURCE_LICENSES.txt", "オープンソースライセンス",
                "https://github.com/empira/PDFsharp/blob/master/LICENSE");
        }

        /// <summary>
        /// ライセンス管理ボタンクリック
        /// </summary>
        private void LicenseManagerButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LicenseManagerDialog
            {
                Owner = this
            };
            dialog.ShowDialog();
            // このPCを解除した場合はライセンス状態が変わるため再読み込み
            LoadLicenseInfo();
        }

        /// <summary>
        /// 購入ボタンクリック
        /// </summary>
        private void PurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            var licenseDialog = new LicenseDialog();
            licenseDialog.Owner = this;
            licenseDialog.ShowDialog();
            
            // ダイアログを閉じた後にライセンス情報を再読み込み
            LoadLicenseInfo();
        }

        /// <summary>
        /// 閉じる
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 法的文書を表示（オフライン優先、フォールバックでオンライン）
        /// </summary>
        private void ShowLegalDocument(string filename, string title, string onlineUrl)
        {
            try
            {
                // ローカルファイルのパスを構築
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var filePath = Path.Combine(appDir, "Resources", "Legal", filename);
                
                // ローカルファイルが存在するか確認
                if (File.Exists(filePath))
                {
                    // ローカルファイルを読み込み
                    var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                    
                    // ビューアーで表示
                    var viewer = new LegalDocumentViewer(title, content);
                    viewer.Owner = this;
                    viewer.ShowDialog();
                }
                else
                {
                    // ファイルが見つからない場合
                    var result = MessageBox.Show(
                        $"{title}のファイルが見つかりませんでした。\n\n" +
                        $"オンライン版を表示しますか？",
                        "確認",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        OpenUrl(onlineUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{title}の表示中にエラーが発生しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// URLを開く
        /// </summary>
        private void OpenUrl(string url)
        {
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
                    $"URLを開けませんでした。\n\n{url}\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
