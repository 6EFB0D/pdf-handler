// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Models;

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// ファイル操作サービスのインターフェース
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 指定フォルダ内のPDFファイル一覧を取得
    /// </summary>
    Task<List<PdfFileInfo>> GetPdfFilesAsync(string folderPath);

    /// <summary>
    /// フォルダツリーを取得
    /// </summary>
    Task<FolderNode> GetFolderTreeAsync(string rootPath);

    /// <summary>
    /// ファイル名を変更
    /// </summary>
    Task<bool> RenameFileAsync(string oldPath, string newPath);

    /// <summary>
    /// ファイルを削除
    /// </summary>
    Task<bool> DeleteFileAsync(string filePath);

    /// <summary>
    /// ファイルをコピー
    /// </summary>
    Task<bool> CopyFileAsync(string sourcePath, string destinationPath);
}
