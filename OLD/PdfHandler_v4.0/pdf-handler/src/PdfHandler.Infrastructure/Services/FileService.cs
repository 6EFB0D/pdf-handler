// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// ファイル操作サービスの実装
/// </summary>
public class FileService : IFileService
{
    private readonly IPdfService _pdfService;

    public FileService(IPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    public async Task<List<PdfFileInfo>> GetPdfFilesAsync(string folderPath)
    {
        var pdfFiles = new List<PdfFileInfo>();

        if (!Directory.Exists(folderPath))
            return pdfFiles;

        var files = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                var pdfInfo = new PdfFileInfo
                {
                    FilePath = file,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                };

                // ページ数とサムネイルを非同期で取得
                try
                {
                    pdfInfo.PageCount = await _pdfService.GetPageCountAsync(file);
                    pdfInfo.ThumbnailData = await _pdfService.GenerateThumbnailAsync(file);
                }
                catch
                {
                    // エラーが発生してもファイル情報は追加
                    pdfInfo.PageCount = 0;
                }

                pdfFiles.Add(pdfInfo);
            }
            catch
            {
                // ファイル読み込みエラーは無視
                continue;
            }
        }

        return pdfFiles;
    }

    public async Task<FolderNode> GetFolderTreeAsync(string rootPath)
    {
        return await Task.Run(() => BuildFolderTree(rootPath));
    }

    private FolderNode BuildFolderTree(string path, FolderNode? parent = null)
    {
        var dirInfo = new DirectoryInfo(path);
        var node = new FolderNode
        {
            Path = path,
            Name = dirInfo.Name,
            Parent = parent
        };

        try
        {
            var subDirs = dirInfo.GetDirectories();
            foreach (var subDir in subDirs)
            {
                // 隠しフォルダやシステムフォルダを除外
                if ((subDir.Attributes & FileAttributes.Hidden) == 0 &&
                    (subDir.Attributes & FileAttributes.System) == 0)
                {
                    var childNode = BuildFolderTree(subDir.FullName, node);
                    node.Children.Add(childNode);
                }
            }
        }
        catch
        {
            // アクセス権限エラーなどは無視
        }

        return node;
    }

    public async Task<bool> RenameFileAsync(string oldPath, string newPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(oldPath))
                    return false;

                if (File.Exists(newPath))
                    return false;

                File.Move(oldPath, newPath);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                File.Delete(filePath);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(sourcePath))
                    return false;

                File.Copy(sourcePath, destinationPath, false);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }
}
