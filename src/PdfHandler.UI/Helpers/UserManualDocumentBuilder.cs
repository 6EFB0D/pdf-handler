using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PdfHandler.UI.Helpers;

/// <summary>
/// 取扱説明書用 FlowDocument（章見出し ■ N.、左ペイン目次用）。
/// 本文先頭の目次はプレーンテキスト（章ジャンプは左ペインのみ）。
/// </summary>
public static class UserManualDocumentBuilder
{
    /// <summary>章番号 1〜13（10〜13 を 1 桁と誤認しない）</summary>
    private static readonly Regex ChapterHeaderRegex = new(
        @"^■ (1[0-3]|[1-9])\. (.+)$",
        RegexOptions.Compiled);

    private static readonly Regex TocLineRegex = new(
        @"^(1[0-3]|[1-9])\. (.+)$",
        RegexOptions.Compiled);

    public sealed class BuildResult
    {
        public required FlowDocument Document { get; init; }

        /// <summary>章見出し段落（■ N. 行）</summary>
        public required IReadOnlyDictionary<int, Paragraph> Chapters { get; init; }

        /// <summary>スクロール先（章見出しを包む Section）</summary>
        public required IReadOnlyDictionary<int, Section> ChapterSections { get; init; }

        public required IReadOnlyList<ManualTocEntry> TocEntries { get; init; }
    }

    public sealed record ManualTocEntry(int ChapterNumber, string DisplayText);

    public static BuildResult Build(string content)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Meiryo UI, Yu Gothic UI"),
            FontSize = 13,
            PagePadding = new Thickness(0),
            ColumnWidth = double.PositiveInfinity
        };

        var chapters = new Dictionary<int, Paragraph>();
        var sections = new Dictionary<int, Section>();
        var lines = (content ?? "").Replace("\r\n", "\n").Split('\n');
        var inTocSection = false;

        foreach (var line in lines)
        {
            if (line == "目次")
                inTocSection = true;

            if (inTocSection && (line.StartsWith("━━", StringComparison.Ordinal) || line.StartsWith("■ ", StringComparison.Ordinal)))
                inTocSection = false;

            if (TryParseChapterHeader(line, out var chapterNum, out _))
            {
                inTocSection = false;
                var headerPara = CreateParagraph();
                headerPara.Inlines.Add(new Run(line) { FontWeight = FontWeights.SemiBold });

                var section = new Section { Margin = new Thickness(0, 10, 0, 0) };
                section.Blocks.Add(headerPara);
                doc.Blocks.Add(section);

                chapters[chapterNum] = headerPara;
                sections[chapterNum] = section;
                continue;
            }

            if (inTocSection && TryParseTocLine(line, out _) && !line.Contains("http", StringComparison.OrdinalIgnoreCase))
            {
                var tocPara = CreateParagraph();
                tocPara.Inlines.Add(new Run(line));
                doc.Blocks.Add(tocPara);
                continue;
            }

            var para = CreateParagraph();
            DocumentHyperlinkFormatter.AppendLineWithHyperlinks(para, line);
            doc.Blocks.Add(para);
        }

        var tocEntries = chapters.Keys.OrderBy(k => k).Select(n =>
        {
            var text = GetParagraphText(chapters[n]);
            TryParseChapterHeader(text, out _, out var title);
            return new ManualTocEntry(n, $"{n}. {title}");
        }).ToList();

        return new BuildResult
        {
            Document = doc,
            Chapters = chapters,
            ChapterSections = sections,
            TocEntries = tocEntries
        };
    }

    private static bool TryParseChapterHeader(string line, out int chapterNum, out string title)
    {
        chapterNum = 0;
        title = "";
        var m = ChapterHeaderRegex.Match(line);
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out chapterNum) || chapterNum is < 1 or > 13)
            return false;
        title = m.Groups[2].Value.Trim();
        return true;
    }

    private static bool TryParseTocLine(string line, out int chapterNum)
    {
        chapterNum = 0;
        var m = TocLineRegex.Match(line);
        return m.Success && int.TryParse(m.Groups[1].Value, out chapterNum) && chapterNum is >= 1 and <= 13;
    }

    private static Paragraph CreateParagraph() =>
        new() { Margin = new Thickness(0, 0, 0, 4), LineHeight = 22 };

    private static string GetParagraphText(Paragraph paragraph)
    {
        var range = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
        return range.Text.TrimEnd('\r', '\n');
    }
}
