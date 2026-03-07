// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Windows;
using System.IO;

namespace PdfHandler.UI.Views
{
    public partial class DetailedInfoDialog : Window
    {
        public DetailedInfoDialog()
        {
            InitializeComponent();
            LoadDetailedInfo();
        }

        private void LoadDetailedInfo()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var location = assembly.Location;
                
                var info = $@"バージョン情報
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

表示バージョン: {version?.Major ?? 0}.{version?.Minor ?? 0}.{version?.Build ?? 0}
内部バージョン: {version?.ToString() ?? "不明"}
ビルド番号: {version?.Revision ?? 0}
リリース日: {(string.IsNullOrEmpty(location) ? "不明" : File.GetLastWriteTime(location).ToString("yyyy年MM月dd日"))}

システム情報
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

OS: {Environment.OSVersion}
.NET Runtime: {Environment.Version}
インストール先: {Path.GetDirectoryName(location) ?? "不明"}

使用ライブラリ
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

• PdfSharp 6.1.1 (MIT License)
• CommunityToolkit.Mvvm 8.2.2 (MIT License)
• Microsoft.Extensions.DependencyInjection 8.0
";
                
                InfoTextBlock.Text = info;
            }
            catch (Exception ex)
            {
                InfoTextBlock.Text = $"情報の取得に失敗しました。\n\n{ex.Message}";
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(InfoTextBlock.Text);
                MessageBox.Show("クリップボードにコピーしました。", "確認", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"コピーに失敗しました。\n\n{ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
