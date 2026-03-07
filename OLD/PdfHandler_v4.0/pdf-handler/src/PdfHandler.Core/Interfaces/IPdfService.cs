// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// PDF操作サービスのインターフェース
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// PDFのページ数を取得
    /// </summary>
    Task<int> GetPageCountAsync(string filePath);

    /// <summary>
    /// PDFの指定ページをレンダリングして画像データを取得
    /// </summary>
    Task<byte[]> RenderPageAsync(string filePath, int pageNumber, int dpi = 96);

    /// <summary>
    /// PDFのサムネイルを生成（第1ページ）
    /// </summary>
    Task<byte[]> GenerateThumbnailAsync(string filePath, int width = 150, int height = 200);

    /// <summary>
    /// PDFファイルをメモリに読み込み（ファイルロック回避）
    /// </summary>
    Task<byte[]> LoadPdfToMemoryAsync(string filePath);
}

/// <summary>
/// PDF結合サービスのインターフェース
/// </summary>
public interface IPdfMergeService
{
    /// <summary>
    /// 複数のPDFファイルを結合
    /// </summary>
    Task<bool> MergePdfsAsync(List<string> sourcePaths, string outputPath, IProgress<int>? progress = null);
}

/// <summary>
/// PDF分割サービスのインターフェース
/// </summary>
public interface IPdfSplitService
{
    /// <summary>
    /// PDFを指定ページ範囲で分割
    /// </summary>
    Task<bool> SplitByRangesAsync(string sourcePath, List<(int Start, int End)> ranges, string outputFolder, string fileNamePattern, IProgress<int>? progress = null);

    /// <summary>
    /// PDFを1ページずつ分割
    /// </summary>
    Task<bool> SplitByPageAsync(string sourcePath, string outputFolder, string fileNamePattern, IProgress<int>? progress = null);

    /// <summary>
    /// PDFを等分割
    /// </summary>
    Task<bool> SplitEquallyAsync(string sourcePath, int parts, string outputFolder, string fileNamePattern, IProgress<int>? progress = null);
}
