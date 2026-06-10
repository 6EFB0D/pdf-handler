using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PdfHandler.UI.Helpers;

/// <summary>
/// プレーンテキスト内の URL をクリック可能なハイパーリンク付き FlowDocument に変換する。
/// </summary>
public static class DocumentHyperlinkFormatter
{
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s\]\)」』、。，．]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static FlowDocument ToFlowDocument(string content)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Meiryo UI, Yu Gothic UI"),
            FontSize = 13,
            PagePadding = new Thickness(0),
            ColumnWidth = double.PositiveInfinity
        };

        if (string.IsNullOrEmpty(content))
            return doc;

        var normalized = content.Replace("\r\n", "\n");
        foreach (var line in normalized.Split('\n'))
        {
            var para = new Paragraph { Margin = new Thickness(0, 0, 0, 4), LineHeight = 22 };
            AppendLineWithHyperlinks(para, line);
            doc.Blocks.Add(para);
        }

        return doc;
    }

    public static void AppendLineWithHyperlinks(Paragraph paragraph, string line)
    {
        var lastIndex = 0;
        foreach (Match match in UrlRegex.Matches(line))
        {
            if (match.Index > lastIndex)
                paragraph.Inlines.Add(new Run(line.Substring(lastIndex, match.Index - lastIndex)));

            var url = match.Value.TrimEnd('.', ',', ';', ':');
            var hyperlink = new Hyperlink(new Run(url))
            {
                NavigateUri = new Uri(url),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 102, 204)),
                TextDecorations = TextDecorations.Underline
            };
            hyperlink.RequestNavigate += (_, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"URLを開けませんでした。\n\n{e.Uri.AbsoluteUri}\n\n{ex.Message}",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                e.Handled = true;
            };
            paragraph.Inlines.Add(hyperlink);
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < line.Length)
            paragraph.Inlines.Add(new Run(line.Substring(lastIndex)));
    }
}
