// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// PDFヘッダ・フッター挿入サービスの実装
/// </summary>
public class HeaderFooterService : IHeaderFooterService
{
    /// <summary>ヘッダ・フッター描画域をクリアする高さ（再適用時の重複防止、最大24ptフォント想定）</summary>
    private const double ClearAreaPt = 40;

    /// <inheritdoc />
    public async Task<bool> AddHeaderFooterAsync(string pdfPath, HeaderFooterSettings settings,
        string? outputPath = null, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(pdfPath))
                {
                    System.Diagnostics.Debug.WriteLine($"ヘッダ・フッター: ファイルが見つかりません: {pdfPath}");
                    return false;
                }

                var finalOutputPath = outputPath ?? pdfPath;
                var outputDir = Path.GetDirectoryName(finalOutputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                var fileBytes = File.ReadAllBytes(pdfPath);
                using var document = PdfReader.Open(new MemoryStream(fileBytes), PdfDocumentOpenMode.Modify);

                int totalPages = document.PageCount;
                if (totalPages == 0) return false;

                var headerFont = CreateFont(settings, isHeader: true);
                var footerFont = CreateFont(settings, isHeader: false);

                for (int i = 0; i < totalPages; i++)
                {
                    var page = document.Pages[i];
                    double width = page.Width.Point;
                    double height = page.Height.Point;

                    using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

                    // 再適用時に以前のヘッダ・フッターが重ならないよう、上下マージン領域を白でクリア
                    gfx.DrawRectangle(XBrushes.White, 0, 0, width, ClearAreaPt);
                    gfx.DrawRectangle(XBrushes.White, 0, Math.Max(0, height - ClearAreaPt), width, ClearAreaPt);

                    if (settings.ShowHeader && !string.IsNullOrWhiteSpace(settings.DocumentTitle))
                    {
                        var headerRect = new XRect(0, settings.HeaderMarginPt, width, height - settings.HeaderMarginPt * 2);
                        var headerFormat = GetStringFormat(settings.HeaderAlignment, isTop: true);
                        gfx.DrawString(settings.DocumentTitle, headerFont, XBrushes.Black, headerRect, headerFormat);
                    }

                    if (settings.ShowFooter && settings.PageNumberFormat != PageNumberFormat.None)
                    {
                        var pageNumberText = FormatPageNumber(i + 1, totalPages, settings.PageNumberFormat);
                        var footerY = height - settings.FooterMarginPt;
                        var footerRect = new XRect(0, footerY - footerFont.Height, width, footerFont.Height);
                        var footerFormat = GetStringFormat(settings.FooterAlignment, isTop: false);
                        gfx.DrawString(pageNumberText, footerFont, XBrushes.Black, footerRect, footerFormat);
                    }

                    progress?.Report((int)((i + 1) / (double)totalPages * 100));
                }

                return SaveWithOverwriteHandling(document, finalOutputPath, pdfPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ヘッダ・フッターエラー: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        });
    }

    private static XFont CreateFont(HeaderFooterSettings settings, bool isHeader)
    {
        var family = isHeader
            ? (string.IsNullOrEmpty(settings.HeaderFontFamily) ? settings.FontFamily : settings.HeaderFontFamily)
            : (string.IsNullOrEmpty(settings.FooterFontFamily) ? settings.FontFamily : settings.FooterFontFamily);
        var size = isHeader
            ? (settings.HeaderFontSize > 0 ? settings.HeaderFontSize : settings.FontSize)
            : (settings.FooterFontSize > 0 ? settings.FooterFontSize : settings.FontSize);
        var options = new XPdfFontOptions(PdfFontEmbedding.EmbedCompleteFontFile);
        try
        {
            return new XFont(family, size, XFontStyleEx.Regular, options);
        }
        catch
        {
            return new XFont("MS Gothic", size, XFontStyleEx.Regular, options);
        }
    }

    private static string FormatPageNumber(int currentPage, int totalPages, PageNumberFormat format)
    {
        return format switch
        {
            PageNumberFormat.Single => currentPage.ToString(),
            PageNumberFormat.Fraction => $"{currentPage}/{totalPages}",
            PageNumberFormat.Hyphen => $"-{currentPage}-",
            _ => ""
        };
    }

    private static XStringFormat GetStringFormat(HeaderFooterAlignment alignment, bool isTop)
    {
        var format = new XStringFormat();
        format.LineAlignment = isTop ? XLineAlignment.Near : XLineAlignment.Far;

        format.Alignment = alignment switch
        {
            HeaderFooterAlignment.Left => XStringAlignment.Near,
            HeaderFooterAlignment.Center => XStringAlignment.Center,
            HeaderFooterAlignment.Right => XStringAlignment.Far,
            _ => XStringAlignment.Center
        };
        return format;
    }

    private static bool SaveWithOverwriteHandling(PdfDocument document, string outputPath, string originalSourcePath)
    {
        if (outputPath == originalSourcePath)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
            document.Save(tempPath);

            int retryCount = 0;
            const int maxRetries = 10;
            while (retryCount < maxRetries)
            {
                try
                {
                    File.Copy(tempPath, originalSourcePath, true);
                    try { File.Delete(tempPath); } catch { }
                    return true;
                }
                catch (IOException)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        System.Diagnostics.Debug.WriteLine($"ファイルがロックされています: {originalSourcePath}");
                        try { File.Delete(tempPath); } catch { }
                        return false;
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        document.Save(outputPath);
        return true;
    }
}
