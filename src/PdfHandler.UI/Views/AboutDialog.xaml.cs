// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Diagnostics;
using PdfHandler.UI.Services;
using PdfHandler.UI.Models;
using PdfHandler.Infrastructure.Helpers;

namespace PdfHandler.UI.Views
{
    /// <summary>
    /// AboutDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutDialog : Window
    {
        private readonly UpdateChecker _updateChecker;
        private UpdateInfo? _currentUpdateInfo;

        public AboutDialog()
        {
            InitializeComponent();
            _updateChecker = new UpdateChecker();
            
            // バージョン情報を読み込み
            LoadVersionInfo();
            
            // 自動的に更新確認（バックグラウンド）
            _ = CheckForUpdatesInBackgroundAsync();
        }

        /// <summary>
        /// バージョン情報を読み込み
        /// </summary>
        private void LoadVersionInfo()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                
                // バージョン表示
                var displayVersion = $"{version?.Major ?? 0}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
                VersionTextBlock.Text = $"バージョン {displayVersion}";
                
                // 著作権
                CopyrightTextBlock.Text = "© 2025-2026 Office Go Plan. All rights reserved.";
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ErrorCodes.UiInitFailed, "バージョン情報の取得に失敗", ex);
            }
        }

        /// <summary>
        /// バックグラウンドで更新確認
        /// </summary>
        private async System.Threading.Tasks.Task CheckForUpdatesInBackgroundAsync()
        {
            try
            {
                // 確認中表示
                UpdateStatusTextBlock.Text = "🔄 更新を確認中...";
                UpdateStatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x6C, 0x75, 0x7D));
                
                // 更新確認
                var updateInfo = await _updateChecker.CheckForUpdatesAsync();
                _currentUpdateInfo = updateInfo;
                
                // 結果を反映
                UpdateUpdateStatus(updateInfo);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ErrorCodes.UpdateCheckFailed, "バックグラウンド更新確認エラー", ex);
                UpdateStatusTextBlock.Text = "";
            }
        }

        /// <summary>
        /// 更新ステータスを更新
        /// </summary>
        private void UpdateUpdateStatus(UpdateInfo updateInfo)
        {
            if (updateInfo.HasError)
            {
                // エラー
                UpdateStatusTextBlock.Text = "⚠️ 更新確認に失敗しました";
                UpdateStatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xDC, 0x35, 0x45));
            }
            else if (updateInfo.IsUpdateAvailable)
            {
                var currentMajor = GetMajorVersion(updateInfo.CurrentVersion);
                var latestMajor  = GetMajorVersion(updateInfo.LatestVersion);
                if (latestMajor > currentMajor)
                {
                    // メジャーアップ → 有償案内（橙色）
                    UpdateStatusTextBlock.Text = $"🆕 v{updateInfo.LatestVersion} 公開（新規ご購入が必要です）";
                    UpdateStatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xFF, 0x8C, 0x00));
                }
                else
                {
                    // 同一メジャー → 無償アップデート（青）
                    UpdateStatusTextBlock.Text = $"🆕 v{updateInfo.LatestVersion} が利用可能です";
                    UpdateStatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x00, 0x7B, 0xFF));
                }
                UpdateButton.Visibility = Visibility.Visible;
            }
            else
            {
                // 最新版
                UpdateStatusTextBlock.Text = "✅ 最新版です";
                UpdateStatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x28, 0xA7, 0x45));
            }
        }

        /// <summary>
        /// 更新を確認ボタン
        /// </summary>
        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 確認中表示
                UpdateStatusTextBlock.Text = "🔄 更新を確認中...";
                UpdateStatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x6C, 0x75, 0x7D));
                UpdateButton.Visibility = Visibility.Collapsed;
                
                // 更新確認
                var updateInfo = await _updateChecker.CheckForUpdatesAsync();
                _currentUpdateInfo = updateInfo;
                
                if (updateInfo.HasError)
                {
                    // エラーダイアログ
                    ShowUpdateErrorDialog(updateInfo);
                }
                else if (updateInfo.IsUpdateAvailable)
                {
                    // 更新ありダイアログ
                    ShowUpdateAvailableDialog(updateInfo);
                }
                else
                {
                    // 最新版ダイアログ
                    ShowLatestVersionDialog(updateInfo);
                }
                
                // ステータス更新
                UpdateUpdateStatus(updateInfo);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ErrorCodes.UpdateCheckFailed, "更新確認エラー", ex);
                MessageBox.Show(
                    ErrorCodes.UserMessage(ErrorCodes.UpdateCheckFailed, "更新確認中にエラーが発生しました。"),
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// アップデートボタン
        /// </summary>
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUpdateInfo != null && _currentUpdateInfo.IsUpdateAvailable)
            {
                ShowUpdateAvailableDialog(_currentUpdateInfo);
            }
        }

        /// <summary>
        /// 最新版ダイアログ
        /// </summary>
        private void ShowLatestVersionDialog(UpdateInfo updateInfo)
        {
            MessageBox.Show(
                $"✅ 最新版をご利用中です\n\n" +
                $"現在のバージョン: {updateInfo.CurrentVersion}\n" +
                $"最新バージョン: {updateInfo.LatestVersion}\n\n" +
                $"最終確認: {DateTime.Now:yyyy年MM月dd日 HH:mm}",
                "更新確認",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// 更新ありダイアログ（メジャーバージョンが異なる場合は有償案内を表示）
        /// </summary>
        private void ShowUpdateAvailableDialog(UpdateInfo updateInfo)
        {
            var currentMajor = GetMajorVersion(updateInfo.CurrentVersion);
            var latestMajor  = GetMajorVersion(updateInfo.LatestVersion);
            var isMajorUpgrade = latestMajor > currentMajor;

            string message;
            string title;
            MessageBoxImage icon;

            if (isMajorUpgrade)
            {
                // メジャーバージョンアップ → 有償案内
                message =
                    $"🆕 新しいメジャーバージョン v{updateInfo.LatestVersion} が公開されています\n\n" +
                    $"現在のバージョン: v{updateInfo.CurrentVersion}\n" +
                    $"最新バージョン:   v{updateInfo.LatestVersion}\n\n" +
                    $"⚠️ ご注意: v{latestMajor}.x.x は現在お持ちのライセンス（v{currentMajor}.x.x 対象）では\n" +
                    $"ご利用いただけません。新しいライセンスのご購入が必要です。\n\n" +
                    $"（利用規約 第8条 / 詳細はリリースページをご確認ください）\n\n" +
                    $"リリースページを開きますか？";
                title = "メジャーバージョンアップのお知らせ";
                icon  = MessageBoxImage.Warning;
            }
            else
            {
                // 同一メジャー内の無償アップデート
                message =
                    $"🆕 新しいバージョンが利用可能です\n\n" +
                    $"現在のバージョン: v{updateInfo.CurrentVersion}\n" +
                    $"最新バージョン:   v{updateInfo.LatestVersion}\n\n" +
                    $"✅ 現在のライセンス（v{currentMajor}.x.x 対象）でそのままご利用いただけます。\n\n" +
                    $"リリース日: {updateInfo.FormattedReleaseDate}\n" +
                    $"ファイルサイズ: {updateInfo.FormattedSize}\n\n" +
                    $"ダウンロードページを開きますか？";
                title = "アップデート";
                icon  = MessageBoxImage.Information;
            }

            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, icon);
            if (result == MessageBoxResult.Yes)
            {
                OpenUrl(updateInfo.DownloadUrl);
            }
        }

        /// <summary>
        /// バージョン文字列からメジャーバージョン番号を取得
        /// </summary>
        private static int GetMajorVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return 0;
            var part = version.TrimStart('v', 'V').Split('.')[0];
            return int.TryParse(part, out var major) ? major : 0;
        }

        /// <summary>
        /// エラーダイアログ
        /// </summary>
        private void ShowUpdateErrorDialog(UpdateInfo updateInfo)
        {
            var result = MessageBox.Show(
                $"⚠️ 更新情報を取得できませんでした\n\n" +
                $"{updateInfo.ErrorMessage}\n\n" +
                $"手動で確認しますか？",
                "更新確認エラー",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                OpenUrl("https://github.com/6EFB0D/pdf-handler/releases");
            }
        }

        /// <summary>
        /// 取扱説明書（オフライン優先）
        /// </summary>
        private void UserManual_Click(object sender, RoutedEventArgs e)
        {
            ShowDocumentOffline("USER_MANUAL.txt", "取扱説明書",
                "https://github.com/6EFB0D/pdf-handler#readme");
        }

        /// <summary>
        /// メールでお問い合わせ
        /// </summary>
        private void Contact_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var settings = app.GetService<PdfHandler.Infrastructure.Configuration.AppSettings>();
            var url = settings.ContactUrl?.Trim();
            if (!string.IsNullOrEmpty(url))
                OpenUrl(url);
            else
                OpenUrl("mailto:support@office-goplan.com");
        }

        /// <summary>
        /// アンケート・ご要望フォーム
        /// </summary>
        private void SurveyForm_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var settings = app.GetService<PdfHandler.Infrastructure.Configuration.AppSettings>();
            var url = settings.SurveyFormUrl?.Trim();
            if (!string.IsNullOrEmpty(url))
                OpenUrl(url);
            else
                OpenUrl("https://forms.gle/placeholder");
        }

        /// <summary>
        /// ライセンス情報
        /// </summary>
        private void License_Click(object sender, RoutedEventArgs e)
        {
            var licenseDialog = new LicenseDialog();
            licenseDialog.Owner = this;
            licenseDialog.ShowDialog();
        }

        /// <summary>
        /// 詳細情報
        /// </summary>
        private void DetailedInfo_Click(object sender, RoutedEventArgs e)
        {
            var detailedDialog = new DetailedInfoDialog();
            detailedDialog.Owner = this;
            detailedDialog.ShowDialog();
        }

        /// <summary>
        /// 閉じる
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// ドキュメントを表示（オフライン優先、フォールバックでオンライン）
        /// </summary>
        private void ShowDocumentOffline(string filename, string title, string onlineUrl)
        {
            try
            {
                // ローカルファイルのパスを構築
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var filePath = Path.Combine(appDir, "Resources", "Docs", filename);
                
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
                DebugLogger.LogError(ErrorCodes.DocumentOpenFailed, $"{title}の表示中にエラー発生", ex);
                MessageBox.Show(
                    ErrorCodes.UserMessage(ErrorCodes.DocumentOpenFailed, $"{title}を表示できませんでした。"),
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
                DebugLogger.LogError(ErrorCodes.UrlOpenFailed, $"URLオープン失敗: {url}", ex);
                MessageBox.Show(
                    ErrorCodes.UserMessage(ErrorCodes.UrlOpenFailed, "URLを開けませんでした。"),
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
