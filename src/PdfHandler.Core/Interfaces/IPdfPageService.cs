// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// PDFページ操作（削除・挿入）サービスのインターフェース
/// </summary>
public interface IPdfPageService
{
    /// <summary>
    /// 指定ページを削除したPDFを出力する
    /// </summary>
    /// <param name="sourcePath">ソースPDFファイルパス</param>
    /// <param name="pageNumbers">削除するページ番号のリスト（1ベース）</param>
    /// <param name="outputPath">出力ファイルパス（nullの場合は元ファイルを上書き）</param>
    /// <param name="progress">進捗報告用（オプション）</param>
    Task<bool> DeletePagesAsync(string sourcePath, IEnumerable<int> pageNumbers, string? outputPath = null, IProgress<int>? progress = null);

    /// <summary>
    /// 指定位置にページを挿入したPDFを出力する
    /// </summary>
    /// <param name="sourcePath">編集対象のPDFファイルパス</param>
    /// <param name="insertPosition">挿入位置（1ベース。1の場合は先頭に挿入、PageCount+1の場合は末尾に追加）</param>
    /// <param name="pageSourcePath">挿入するページの元PDFファイルパス</param>
    /// <param name="sourcePageNumber">挿入する元PDFのページ番号（1ベース）</param>
    /// <param name="outputPath">出力ファイルパス（nullの場合は元ファイルを上書き）</param>
    /// <param name="progress">進捗報告用（オプション）</param>
    Task<bool> InsertPageAsync(string sourcePath, int insertPosition, string pageSourcePath, int sourcePageNumber, string? outputPath = null, IProgress<int>? progress = null);

    /// <summary>
    /// 指定位置に別PDFの全ページを挿入したPDFを出力する
    /// </summary>
    /// <param name="sourcePath">編集対象のPDFファイルパス</param>
    /// <param name="insertPosition">挿入位置（1ベース）</param>
    /// <param name="pdfToInsertPath">挿入するPDFファイルパス</param>
    /// <param name="outputPath">出力ファイルパス（nullの場合は元ファイルを上書き）</param>
    /// <param name="progress">進捗報告用（オプション）</param>
    Task<bool> InsertPdfAsync(string sourcePath, int insertPosition, string pdfToInsertPath, string? outputPath = null, IProgress<int>? progress = null);
}
