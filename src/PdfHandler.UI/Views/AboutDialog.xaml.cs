// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Diagnostics;
using PdfHandler.UI.Services;
using PdfHandler.UI.Models;

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
                CopyrightTextBlock.Text = "© 2024-2025 Goplan. All rights reserved.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"バージョン情報の取得に失敗: {ex.Message}");
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
                Debug.WriteLine($"更新確認エラー: {ex.Message}");
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
                // 更新あり
                UpdateStatusTextBlock.Text = $"🆕 v{updateInfo.LatestVersion} が利用可能です";
                UpdateStatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x00, 0x7B, 0xFF));
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
                MessageBox.Show(
                    $"更新確認中にエラーが発生しました。\n\n{ex.Message}",
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
        /// 更新ありダイアログ
        /// </summary>
        private void ShowUpdateAvailableDialog(UpdateInfo updateInfo)
        {
            var result = MessageBox.Show(
                $"🆕 新しいバージョンが利用可能です\n\n" +
                $"現在のバージョン: {updateInfo.CurrentVersion}\n" +
                $"最新バージョン: {updateInfo.LatestVersion}\n\n" +
                $"リリース日: {updateInfo.FormattedReleaseDate}\n" +
                $"ファイルサイズ: {updateInfo.FormattedSize}\n\n" +
                $"ダウンロードページを開きますか？",
                "アップデート",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            
            if (result == MessageBoxResult.Yes)
            {
                OpenUrl(updateInfo.DownloadUrl);
            }
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
        /// よくある質問
        /// </summary>
        private void FAQ_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/6EFB0D/pdf-handler/discussions/categories/q-a");
        }

        /// <summary>
        /// 問題を報告
        /// </summary>
        private void ReportIssue_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/6EFB0D/pdf-handler/issues/new/choose");
        }

        /// <summary>
        /// ご意見・ご要望
        /// </summary>
        private void Discussions_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/6EFB0D/pdf-handler/discussions");
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
