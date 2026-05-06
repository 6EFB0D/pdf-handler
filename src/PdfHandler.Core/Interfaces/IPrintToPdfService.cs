// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// プリンタドライバサービスのインターフェース
/// </summary>
public interface IPrintToPdfService
{
    /// <summary>
    /// ファイル名パターン（元のファイル名またはタイムスタンプ）
    /// </summary>
    enum FileNamePattern
    {
        OriginalFileName,
        Timestamp
    }

    /// <summary>
    /// ファイル名パターンを設定
    /// </summary>
    Task SetFileNamePatternAsync(FileNamePattern pattern);

    /// <summary>
    /// 現在のファイル名パターンを取得
    /// </summary>
    FileNamePattern GetFileNamePattern();

    /// <summary>
    /// PDFファイルを印刷（専用ワークフォルダに保存）
    /// </summary>
    Task<bool> PrintToPdfAsync(string sourceFilePath);

    /// <summary>
    /// 複数のPDFファイルを印刷（専用ワークフォルダに保存）
    /// </summary>
    Task<bool> PrintMultipleToPdfAsync(List<string> sourceFilePaths, IProgress<int>? progress = null);
}






