// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Windows;
using PdfHandler.UI.Services;
using PdfHandler.UI.Models;
using PdfHandler.Infrastructure.Helpers;
using PdfHandler.Infrastructure.Configuration;

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
            RefreshStartupNotificationButton();

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

                if (Application.Current is App app)
                {
                    var settings = app.GetService<AppSettings>();
                    if (settings.IsDevEnvironment)
                    {
                        EnvironmentTextBlock.Text =
                            "⚠ 開発環境 (DEV) — " + AppEnvironmentResolver.GetConnectionLabel(settings);
                        EnvironmentTextBlock.Visibility = Visibility.Visible;
                    }
                }
                
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
                if (UpdateDialogHelper.IsMajorUpgrade(updateInfo))
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

        private void RefreshStartupNotificationButton()
        {
            var store = new UpdateNotificationStore();
            ReEnableStartupNotificationButton.Visibility = store.IsSuppressed
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ReEnableStartupNotification_Click(object sender, RoutedEventArgs e)
        {
            var store = new UpdateNotificationStore();
            if (!store.IsSuppressed)
            {
                MessageBox.Show(
                    this,
                    "起動時の更新お知らせは、すでに有効です。\nお知らせを止めた場合は、起動時のダイアログで「次回以降表示しない」をオンにしてください。",
                    "更新のお知らせ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                RefreshStartupNotificationButton();
                return;
            }

            store.ClearSuppression();
            RefreshStartupNotificationButton();
            MessageBox.Show(
                this,
                "起動時の更新お知らせを再有効化しました。\n新しいバージョンが公開されれば、次回起動時にお知らせします。",
                "更新のお知らせ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
                    UpdateDialogHelper.ShowUpdateErrorDialog(updateInfo, this);
                }
                else if (updateInfo.IsUpdateAvailable)
                {
                    var result = UpdateDialogHelper.ShowUpdateAvailableDialog(updateInfo, this);
                    if (result == MessageBoxResult.Yes)
                        UpdateDialogHelper.OpenUrl(updateInfo.DownloadUrl);
                }
                else
                {
                    UpdateDialogHelper.ShowLatestVersionDialog(updateInfo, this);
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
                var result = UpdateDialogHelper.ShowUpdateAvailableDialog(_currentUpdateInfo, this);
                if (result == MessageBoxResult.Yes)
                    UpdateDialogHelper.OpenUrl(_currentUpdateInfo.DownloadUrl);
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
                UpdateDialogHelper.OpenUrl(url);
            else
                UpdateDialogHelper.OpenUrl("mailto:support@office-goplan.com");
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
                UpdateDialogHelper.OpenUrl(url);
            else
                UpdateDialogHelper.OpenUrl("https://forms.gle/placeholder");
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
                    
                    var enableChapterNav = filename.Equals("USER_MANUAL.txt", StringComparison.OrdinalIgnoreCase);
                    var viewer = new LegalDocumentViewer(title, content, enableChapterNav);
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
                        UpdateDialogHelper.OpenUrl(onlineUrl);
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

    }
}
