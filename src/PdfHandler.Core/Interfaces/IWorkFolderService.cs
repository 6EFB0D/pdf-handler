// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// 専用ワークフォルダサービスのインターフェース
/// </summary>
public interface IWorkFolderService
{
    /// <summary>
    /// ワークフォルダのパスを取得
    /// </summary>
    string GetWorkFolderPath();

    /// <summary>
    /// ワークフォルダのパスを設定
    /// </summary>
    Task SetWorkFolderPathAsync(string path);

    /// <summary>
    /// ワークフォルダが存在するかどうかを確認
    /// </summary>
    bool WorkFolderExists();

    /// <summary>
    /// ワークフォルダを作成
    /// </summary>
    Task<bool> CreateWorkFolderAsync();

    /// <summary>
    /// ワークフォルダにファイルを保存
    /// </summary>
    Task<string> SaveToWorkFolderAsync(string sourceFilePath, string? fileName = null);
}






