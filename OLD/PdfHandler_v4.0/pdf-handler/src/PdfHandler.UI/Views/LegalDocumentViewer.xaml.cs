// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;

namespace PdfHandler.UI.Views
{
    /// <summary>
    /// LegalDocumentViewer.xaml の相互作用ロジック
    /// 法的文書（利用規約、プライバシーポリシー等）を表示するビューアー
    /// </summary>
    public partial class LegalDocumentViewer : Window
    {
        private readonly string _documentTitle;
        private readonly string _documentContent;

        public LegalDocumentViewer(string title, string content)
        {
            InitializeComponent();
            
            _documentTitle = title;
            _documentContent = content;
            
            // タイトルと内容を設定
            Title = title;
            TitleTextBlock.Text = title;
            ContentTextBlock.Text = content;
        }

        /// <summary>
        /// コピーボタン
        /// </summary>
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_documentContent);
                MessageBox.Show(
                    "クリップボードにコピーしました。",
                    "確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"コピーに失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存ボタン
        /// </summary>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "テキストファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*",
                    FileName = $"{_documentTitle}.txt",
                    DefaultExt = ".txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, _documentContent, System.Text.Encoding.UTF8);
                    MessageBox.Show(
                        "保存しました。",
                        "確認",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"保存に失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 印刷ボタン
        /// </summary>
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // 印刷用のFlowDocumentを作成
                    var flowDoc = new FlowDocument
                    {
                        PagePadding = new Thickness(50),
                        ColumnWidth = double.PositiveInfinity // 1カラム
                    };
                    
                    // タイトル
                    var titlePara = new Paragraph();
                    titlePara.Inlines.Add(new Run(_documentTitle)
                    {
                        FontSize = 18,
                        FontWeight = FontWeights.Bold
                    });
                    flowDoc.Blocks.Add(titlePara);
                    
                    // 区切り線
                    flowDoc.Blocks.Add(new Paragraph(new Run(new string('─', 60))));
                    
                    // 本文
                    var contentPara = new Paragraph();
                    contentPara.Inlines.Add(new Run(_documentContent)
                    {
                        FontSize = 12
                    });
                    flowDoc.Blocks.Add(contentPara);
                    
                    // 印刷実行
                    IDocumentPaginatorSource idpSource = flowDoc;
                    printDialog.PrintDocument(idpSource.DocumentPaginator, _documentTitle);
                    
                    MessageBox.Show(
                        "印刷を開始しました。",
                        "確認",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"印刷に失敗しました。\n\n{ex.Message}",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 閉じるボタン
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
