// PDFハンドラ (PDF Handler)

// Copyright (c) 2025-2026 Goplan. All rights reserved.

// Licensed under the MIT License. See LICENSE file in the project root for full license information.



using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Documents;

using System.Windows.Input;

using System.Windows.Threading;

using Microsoft.Win32;

using PdfHandler.UI.Helpers;



namespace PdfHandler.UI.Views

{

    /// <summary>

    /// 法的文書（利用規約、プライバシーポリシー等）を表示するビューアー

    /// </summary>

    public partial class LegalDocumentViewer : Window

    {

        private readonly string _documentTitle;

        private readonly string _documentContent;

        private IReadOnlyDictionary<int, Section>? _chapterSections;

        private bool _suppressTocSelectionChanged;

        private Dictionary<int, double>? _chapterScrollOffsets;

        private bool _chapterOffsetCacheScheduled;



        public LegalDocumentViewer(string title, string content, bool enableChapterNavigation = false)

        {

            InitializeComponent();



            _documentTitle = title;

            _documentContent = content;



            Title = title;

            TitleTextBlock.Text = title;



            if (enableChapterNavigation)

            {

                var built = UserManualDocumentBuilder.Build(content);

                ContentRichTextBox.Document = built.Document;

                _chapterSections = built.ChapterSections;

                SetupChapterNavigation(built.TocEntries);

                ContentRichTextBox.Loaded += (_, _) => ScheduleChapterOffsetCache();

            }

            else

            {

                ContentRichTextBox.Document = DocumentHyperlinkFormatter.ToFlowDocument(content);

            }



            PreviewKeyDown += OnPreviewKeyDown;

            Loaded += (_, _) =>

            {

                ContentScrollViewer.Focus();

                Keyboard.Focus(ContentScrollViewer);

            };

        }



        private void SetupChapterNavigation(IReadOnlyList<UserManualDocumentBuilder.ManualTocEntry> tocEntries)

        {

            TocColumn.Width = new GridLength(200);

            TocPanelBorder.Visibility = Visibility.Visible;

            Width = Math.Max(Width, 980);



            ChapterListBox.ItemsSource = tocEntries;

            ChapterListBox.DisplayMemberPath = nameof(UserManualDocumentBuilder.ManualTocEntry.DisplayText);

        }



        private void ChapterListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {

            if (_suppressTocSelectionChanged ||

                ChapterListBox.SelectedItem is not UserManualDocumentBuilder.ManualTocEntry entry ||

                _chapterSections == null ||

                !_chapterSections.TryGetValue(entry.ChapterNumber, out var section))

                return;



            ScrollToChapter(section);

        }



        private void ScheduleChapterOffsetCache()
        {
            if (_chapterSections == null || _chapterOffsetCacheScheduled)
                return;

            _chapterOffsetCacheScheduled = true;
            Dispatcher.BeginInvoke(() => CacheChapterScrollOffsets(), DispatcherPriority.ApplicationIdle);
        }

        private void ForceDocumentLayout()
        {
            ContentScrollViewer.UpdateLayout();
            ContentRichTextBox.UpdateLayout();

            var padding = ContentScrollViewer.Padding;
            var width = ContentScrollViewer.ViewportWidth;
            if (width <= 0 || double.IsNaN(width))
                width = ContentScrollViewer.ActualWidth - padding.Left - padding.Right;
            width = Math.Max(width, 400);

            ContentRichTextBox.Measure(new Size(width, double.PositiveInfinity));
            ContentRichTextBox.Arrange(new Rect(0, 0, width, ContentRichTextBox.DesiredSize.Height));
            ContentRichTextBox.UpdateLayout();
        }

        private void CacheChapterScrollOffsets()
        {
            if (_chapterSections == null)
                return;

            var offsets = new Dictionary<int, double>();
            var previousOpacity = ContentRichTextBox.Opacity;
            ContentRichTextBox.Opacity = 0;

            try
            {
                ForceDocumentLayout();

                foreach (var chapterNumber in _chapterSections.Keys.OrderBy(k => k))
                {
                    var section = _chapterSections[chapterNumber];
                    if (section.Blocks.FirstBlock is not Paragraph header)
                        continue;

                    section.BringIntoView();
                    ContentRichTextBox.UpdateLayout();
                    ContentScrollViewer.UpdateLayout();

                    var start = header.ContentStart;
                    ContentRichTextBox.Selection.Select(start, start);

                    try
                    {
                        var rect = ContentRichTextBox.Selection.Start.GetCharacterRect(LogicalDirection.Forward);
                        if (!double.IsNaN(rect.Top) && !double.IsInfinity(rect.Top))
                        {
                            var pt = ContentRichTextBox.TranslatePoint(new Point(0, rect.Top), ContentScrollViewer);
                            offsets[chapterNumber] = Math.Max(0, pt.Y - 24);
                        }
                    }
                    catch
                    {
                        // ApplyChapterScroll falls back to BringIntoView
                    }
                }

                ContentScrollViewer.ScrollToVerticalOffset(0);
                ContentRichTextBox.Selection.Select(
                    ContentRichTextBox.Document.ContentStart,
                    ContentRichTextBox.Document.ContentStart);

                _chapterScrollOffsets = offsets;
            }
            finally
            {
                ContentRichTextBox.Opacity = previousOpacity;
            }
        }

        private void ScrollToChapterNumber(int chapterNumber)
        {
            if (_chapterScrollOffsets == null)
                CacheChapterScrollOffsets();

            ApplyChapterScroll(chapterNumber);
            SyncListBoxSelection(chapterNumber);
            ContentScrollViewer.Focus();
            Keyboard.Focus(ContentScrollViewer);
        }

        private void ApplyChapterScroll(int chapterNumber)
        {
            if (_chapterScrollOffsets != null &&
                _chapterScrollOffsets.TryGetValue(chapterNumber, out var offset))
            {
                ContentScrollViewer.ScrollToVerticalOffset(offset);
            }
            else if (_chapterSections?.TryGetValue(chapterNumber, out var section) == true)
            {
                section.BringIntoView();
            }
        }

        private void TocHeader_Click(object sender, RoutedEventArgs e)
        {
            ScrollToDocumentTop();
        }

        private void ScrollToDocumentTop()
        {
            ContentScrollViewer.ScrollToVerticalOffset(0);

            _suppressTocSelectionChanged = true;
            ChapterListBox.SelectedIndex = -1;
            _suppressTocSelectionChanged = false;

            ContentScrollViewer.Focus();
            Keyboard.Focus(ContentScrollViewer);
        }

        private void ScrollToChapter(Section section)
        {
            if (_chapterSections == null)
                return;

            foreach (var kv in _chapterSections)
            {
                if (ReferenceEquals(kv.Value, section))
                {
                    ScrollToChapterNumber(kv.Key);
                    return;
                }
            }
        }

        private void SyncListBoxSelection(int chapterNumber)
        {
            if (ChapterListBox.ItemsSource == null)
                return;

            foreach (var item in ChapterListBox.Items)
            {
                if (item is UserManualDocumentBuilder.ManualTocEntry tocEntry &&
                    tocEntry.ChapterNumber == chapterNumber)
                {
                    _suppressTocSelectionChanged = true;
                    ChapterListBox.SelectedItem = item;
                    _suppressTocSelectionChanged = false;
                    break;
                }
            }
        }



        private void OnPreviewKeyDown(object sender, KeyEventArgs e)

        {

            if (ContentScrollViewer == null)

                return;



            var pageStep = Math.Max(ContentScrollViewer.ViewportHeight * 0.9, 48);

            var offset = ContentScrollViewer.VerticalOffset;

            var maxOffset = ContentScrollViewer.ScrollableHeight;



            switch (e.Key)

            {

                case Key.PageDown:

                    ContentScrollViewer.ScrollToVerticalOffset(Math.Min(offset + pageStep, maxOffset));

                    e.Handled = true;

                    break;

                case Key.PageUp:

                    ContentScrollViewer.ScrollToVerticalOffset(Math.Max(0, offset - pageStep));

                    e.Handled = true;

                    break;

            }

        }



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



        private void Print_Click(object sender, RoutedEventArgs e)

        {

            try

            {

                var printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)

                {

                    var flowDoc = new FlowDocument

                    {

                        PagePadding = new Thickness(50),

                        ColumnWidth = double.PositiveInfinity

                    };



                    var titlePara = new Paragraph();

                    titlePara.Inlines.Add(new Run(_documentTitle)

                    {

                        FontSize = 18,

                        FontWeight = FontWeights.Bold

                    });

                    flowDoc.Blocks.Add(titlePara);

                    flowDoc.Blocks.Add(new Paragraph(new Run(new string('─', 60))));



                    var contentPara = new Paragraph();

                    contentPara.Inlines.Add(new Run(_documentContent) { FontSize = 12 });

                    flowDoc.Blocks.Add(contentPara);



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



        private void Close_Click(object sender, RoutedEventArgs e)

        {

            Close();

        }

    }

}

