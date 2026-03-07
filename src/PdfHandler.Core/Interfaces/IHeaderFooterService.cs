// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Models;

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// PDFヘッダ・フッター挿入サービスのインターフェース
/// </summary>
public interface IHeaderFooterService
{
    /// <summary>
    /// PDFの全ページにヘッダ・フッターを描画する
    /// </summary>
    /// <param name="pdfPath">対象PDFのパス</param>
    /// <param name="settings">ヘッダ・フッター設定</param>
    /// <param name="outputPath">出力パス（nullの場合は元ファイルを上書き）</param>
    /// <param name="progress">進捗報告用（オプション）</param>
    Task<bool> AddHeaderFooterAsync(string pdfPath, HeaderFooterSettings settings,
        string? outputPath = null, IProgress<int>? progress = null);
}
