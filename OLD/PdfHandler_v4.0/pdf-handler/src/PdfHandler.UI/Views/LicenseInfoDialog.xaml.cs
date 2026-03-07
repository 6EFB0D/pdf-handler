// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Windows;
using System.Diagnostics;

namespace PdfHandler.UI.Views
{
    /// <summary>
    /// LicenseInfoDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class LicenseInfoDialog : Window
    {
        public LicenseInfoDialog()
        {
            InitializeComponent();
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
