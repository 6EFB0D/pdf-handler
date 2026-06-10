using System;
using System.IO;
using System.Reflection;
using System.Windows;
using PdfHandler.UI.Views;

namespace PdfHandler.UI.Helpers;

/// <summary>
/// 利用規約・プライバシーポリシー等の埋め込みリソース表示（Public repo の raw ファイルは参照しない）。
/// </summary>
public static class LegalDocumentHelper
{
    public static void Show(Window owner, string filename, string title, string onlineFallbackUrl)
    {
        try
        {
            var content = TryReadEmbedded(filename);
            if (content != null)
            {
                new LegalDocumentViewer(title, content) { Owner = owner }.ShowDialog();
                return;
            }

            if (MessageBox.Show(
                    $"{title}のファイルが見つかりませんでした。\n\nオンライン（Office Go Plan）を表示しますか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                BrowserHelper.OpenUrl(onlineFallbackUrl);
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

    public static string? TryReadEmbedded(string filename)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"PdfHandler.UI.Resources.Legal.{filename}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
