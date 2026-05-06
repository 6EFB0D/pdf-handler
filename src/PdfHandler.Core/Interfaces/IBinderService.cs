// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Models;

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// PDFバインダーサービスのインターフェース
/// </summary>
public interface IBinderService
{
    /// <summary>
    /// すべてのバインダーを取得
    /// </summary>
    Task<List<Binder>> GetAllBindersAsync();

    /// <summary>
    /// バインダーを取得
    /// </summary>
    Task<Binder?> GetBinderAsync(string binderId);

    /// <summary>
    /// バインダーを作成
    /// </summary>
    Task<Binder> CreateBinderAsync(string name, string? description = null);

    /// <summary>
    /// バインダーを更新
    /// </summary>
    Task<bool> UpdateBinderAsync(Binder binder);

    /// <summary>
    /// バインダーを削除
    /// </summary>
    Task<bool> DeleteBinderAsync(string binderId);

    /// <summary>
    /// バインダーにPDFファイルを追加
    /// </summary>
    Task<bool> AddPdfToBinderAsync(string binderId, string pdfFilePath);

    /// <summary>
    /// バインダーからPDFファイルを削除
    /// </summary>
    Task<bool> RemovePdfFromBinderAsync(string binderId, string pdfFilePath);

    /// <summary>
    /// バインダー内のPDFファイルの順序を変更
    /// </summary>
    Task<bool> ReorderPdfFilesAsync(string binderId, List<string> orderedFilePaths);

    /// <summary>
    /// バインダーをPDFファイルに結合
    /// </summary>
    Task<bool> MergeBinderToPdfAsync(string binderId, string outputPath, IProgress<int>? progress = null);
}






