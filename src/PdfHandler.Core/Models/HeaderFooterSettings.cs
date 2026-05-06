// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Models;

/// <summary>
/// ページ番号の表示形式
/// </summary>
public enum PageNumberFormat
{
    None,
    Single,   // 1
    Fraction, // 1/10
    Hyphen    // -1-
}

/// <summary>
/// ヘッダ・フッターの配置
/// </summary>
public enum HeaderFooterAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// ヘッダ・フッター挿入の設定
/// </summary>
public class HeaderFooterSettings
{
    /// <summary>ヘッダーに表示する文書タイトル</summary>
    public string DocumentTitle { get; set; } = "";

    /// <summary>ヘッダーを表示する</summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>フッターを表示する</summary>
    public bool ShowFooter { get; set; } = true;

    /// <summary>ページ番号の表示形式</summary>
    public PageNumberFormat PageNumberFormat { get; set; } = PageNumberFormat.Fraction;

    /// <summary>ヘッダー用フォント名</summary>
    public string HeaderFontFamily { get; set; } = "MS Gothic";

    /// <summary>ヘッダー用フォントサイズ（pt）</summary>
    public double HeaderFontSize { get; set; } = 9;

    /// <summary>フッター用フォント名</summary>
    public string FooterFontFamily { get; set; } = "MS Gothic";

    /// <summary>フッター用フォントサイズ（pt）</summary>
    public double FooterFontSize { get; set; } = 9;

    /// <summary>旧設定との互換用フォント名（HeaderFooterFontFamily未設定時に使用）</summary>
    public string FontFamily { get; set; } = "MS Gothic";

    /// <summary>旧設定との互換用フォントサイズ</summary>
    public double FontSize { get; set; } = 9;

    /// <summary>ヘッダーの配置</summary>
    public HeaderFooterAlignment HeaderAlignment { get; set; } = HeaderFooterAlignment.Center;

    /// <summary>フッターの配置</summary>
    public HeaderFooterAlignment FooterAlignment { get; set; } = HeaderFooterAlignment.Center;

    /// <summary>ヘッダーの余白（ページ上端からのpt）</summary>
    public double HeaderMarginPt { get; set; } = 12;

    /// <summary>フッターの余白（ページ下端からのpt）</summary>
    public double FooterMarginPt { get; set; } = 12;

    /// <summary>ページ編集時に自動再適用する</summary>
    public bool AutoReapplyOnPageEdit { get; set; } = true;
}
