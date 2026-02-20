// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// PDF回転サービスのインターフェース
/// </summary>
public interface IPdfRotateService
{
    /// <summary>
    /// PDFの指定ページを回転
    /// </summary>
    /// <param name="filePath">PDFファイルパス</param>
    /// <param name="pageNumber">ページ番号（1ベース）</param>
    /// <param name="rotationDegrees">回転角度（90, 180, 270）</param>
    /// <param name="outputPath">出力ファイルパス（nullの場合は上書き）</param>
    Task<bool> RotatePageAsync(string filePath, int pageNumber, int rotationDegrees, string? outputPath = null);

    /// <summary>
    /// PDFの全ページを回転
    /// </summary>
    /// <param name="filePath">PDFファイルパス</param>
    /// <param name="rotationDegrees">回転角度（90, 180, 270）</param>
    /// <param name="outputPath">出力ファイルパス（nullの場合は上書き）</param>
    Task<bool> RotateAllPagesAsync(string filePath, int rotationDegrees, string? outputPath = null);
}






