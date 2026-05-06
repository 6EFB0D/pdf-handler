// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfHandler.Core.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// PDFページ操作（削除・挿入）サービスの実装
/// </summary>
public class PdfPageService : IPdfPageService
{
    /// <inheritdoc />
    public async Task<bool> DeletePagesAsync(string sourcePath, IEnumerable<int> pageNumbers, string? outputPath = null, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            PdfDocument? sourceDocument = null;

            try
            {
                if (!File.Exists(sourcePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ソースファイルが見つかりません: {sourcePath}");
                    return false;
                }

                var pagesToDelete = pageNumbers.Select(p => p).Distinct().OrderBy(p => p).ToHashSet();
                if (pagesToDelete.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("削除対象のページが指定されていません");
                    return false;
                }

                var finalOutputPath = outputPath ?? sourcePath;
                var outputDir = Path.GetDirectoryName(finalOutputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                sourceDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                int totalPages = sourceDocument.PageCount;

                // 1ベースのページ番号を0ベースのインデックスに変換し、有効性を検証
                var indicesToDelete = new HashSet<int>();
                foreach (var pageNum in pagesToDelete)
                {
                    if (pageNum < 1 || pageNum > totalPages)
                    {
                        System.Diagnostics.Debug.WriteLine($"無効なページ番号: {pageNum} (総ページ数: {totalPages})");
                        return false;
                    }
                    indicesToDelete.Add(pageNum - 1);
                }

                using var outputDocument = new PdfDocument();

                for (int i = 0; i < totalPages; i++)
                {
                    if (!indicesToDelete.Contains(i))
                    {
                        var page = sourceDocument.Pages[i];
                        outputDocument.AddPage(page);
                    }
                    progress?.Report((int)((i + 1) / (double)totalPages * 100));
                }

                if (outputDocument.PageCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("削除後のページが0になりました");
                    return false;
                }

                return SaveWithOverwriteHandling(outputDocument, finalOutputPath, sourcePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ページ削除エラー: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                sourceDocument?.Dispose();
            }
        });
    }

    /// <inheritdoc />
    public async Task<bool> InsertPageAsync(string sourcePath, int insertPosition, string pageSourcePath, int sourcePageNumber, string? outputPath = null, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            PdfDocument? mainDocument = null;
            PdfDocument? insertSourceDocument = null;

            try
            {
                if (!File.Exists(sourcePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ソースファイルが見つかりません: {sourcePath}");
                    return false;
                }

                if (!File.Exists(pageSourcePath))
                {
                    System.Diagnostics.Debug.WriteLine($"挿入元ファイルが見つかりません: {pageSourcePath}");
                    return false;
                }

                var finalOutputPath = outputPath ?? sourcePath;
                var outputDir = Path.GetDirectoryName(finalOutputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                mainDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                insertSourceDocument = PdfReader.Open(pageSourcePath, PdfDocumentOpenMode.Import);

                int mainPageCount = mainDocument.PageCount;
                int insertSourcePageCount = insertSourceDocument.PageCount;

                if (insertPosition < 1 || insertPosition > mainPageCount + 1)
                {
                    System.Diagnostics.Debug.WriteLine($"無効な挿入位置: {insertPosition} (総ページ数: {mainPageCount})");
                    return false;
                }

                if (sourcePageNumber < 1 || sourcePageNumber > insertSourcePageCount)
                {
                    System.Diagnostics.Debug.WriteLine($"無効な挿入元ページ番号: {sourcePageNumber} (総ページ数: {insertSourcePageCount})");
                    return false;
                }

                var pageToInsert = insertSourceDocument.Pages[sourcePageNumber - 1];
                int insertIndex = insertPosition - 1;

                using var outputDocument = new PdfDocument();

                // 挿入位置より前のページを追加
                for (int i = 0; i < insertIndex; i++)
                {
                    outputDocument.AddPage(mainDocument.Pages[i]);
                    progress?.Report((int)((i + 1) / (double)(mainPageCount + 1) * 100));
                }

                // 挿入ページを追加
                outputDocument.AddPage(pageToInsert);
                progress?.Report((int)(insertIndex + 1) / (mainPageCount + 1) * 100);

                // 挿入位置以降のページを追加
                for (int i = insertIndex; i < mainPageCount; i++)
                {
                    outputDocument.AddPage(mainDocument.Pages[i]);
                    progress?.Report((int)((i + 2) / (double)(mainPageCount + 1) * 100));
                }

                return SaveWithOverwriteHandling(outputDocument, finalOutputPath, sourcePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ページ挿入エラー: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                mainDocument?.Dispose();
                insertSourceDocument?.Dispose();
            }
        });
    }

    /// <inheritdoc />
    public async Task<bool> InsertPdfAsync(string sourcePath, int insertPosition, string pdfToInsertPath, string? outputPath = null, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            PdfDocument? mainDocument = null;
            PdfDocument? insertSourceDocument = null;

            try
            {
                if (!File.Exists(sourcePath) || !File.Exists(pdfToInsertPath))
                    return false;

                var finalOutputPath = outputPath ?? sourcePath;
                var outputDir = Path.GetDirectoryName(finalOutputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                mainDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                insertSourceDocument = PdfReader.Open(pdfToInsertPath, PdfDocumentOpenMode.Import);

                int mainPageCount = mainDocument.PageCount;
                int insertPageCount = insertSourceDocument.PageCount;

                if (insertPosition < 1 || insertPosition > mainPageCount + 1 || insertPageCount == 0)
                    return false;

                int insertIndex = insertPosition - 1;
                int totalOps = mainPageCount + insertPageCount;
                int opCount = 0;

                using var outputDocument = new PdfDocument();

                for (int i = 0; i < insertIndex; i++)
                {
                    outputDocument.AddPage(mainDocument.Pages[i]);
                    progress?.Report((int)(++opCount / (double)totalOps * 100));
                }
                for (int i = 0; i < insertPageCount; i++)
                {
                    outputDocument.AddPage(insertSourceDocument.Pages[i]);
                    progress?.Report((int)(++opCount / (double)totalOps * 100));
                }
                for (int i = insertIndex; i < mainPageCount; i++)
                {
                    outputDocument.AddPage(mainDocument.Pages[i]);
                    progress?.Report((int)(++opCount / (double)totalOps * 100));
                }

                return SaveWithOverwriteHandling(outputDocument, finalOutputPath, sourcePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF挿入エラー: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            finally
            {
                mainDocument?.Dispose();
                insertSourceDocument?.Dispose();
            }
        });
    }

    private static bool SaveWithOverwriteHandling(PdfDocument document, string outputPath, string originalSourcePath)
    {
        if (outputPath == originalSourcePath)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
            document.Save(tempPath);

            int retryCount = 0;
            const int maxRetries = 10;
            while (retryCount < maxRetries)
            {
                try
                {
                    File.Copy(tempPath, originalSourcePath, true);
                    try { File.Delete(tempPath); } catch { }
                    return true;
                }
                catch (IOException)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        System.Diagnostics.Debug.WriteLine($"ファイルがロックされています: {originalSourcePath}");
                        try { File.Delete(tempPath); } catch { }
                        return false;
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        document.Save(outputPath);
        return true;
    }
}
