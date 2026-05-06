// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PdfHandler.Core.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// PDF分割サービスの実装（PdfSharp版 - 完全無償）
/// </summary>
public class PdfSplitService : IPdfSplitService
{
    public async Task<bool> SplitByRangesAsync(string sourcePath, List<(int Start, int End)> ranges, 
        string outputFolder, string fileNamePattern, IProgress<int>? progress = null)
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

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                System.Diagnostics.Debug.WriteLine($"PDF分割開始: {Path.GetFileName(sourcePath)} → {ranges.Count}ファイル");

                // ソースPDFを開く（Importモード）
                sourceDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                int totalPages = sourceDocument.PageCount;
                System.Diagnostics.Debug.WriteLine($"  総ページ数: {totalPages}");

                for (int i = 0; i < ranges.Count; i++)
                {
                    var (start, end) = ranges[i];
                    
                    // ページ番号は1-basedで指定される
                    // PdfSharpは0-basedなので-1する
                    if (start < 1 || end > totalPages || start > end)
                    {
                        System.Diagnostics.Debug.WriteLine($"  無効な範囲: {start}-{end}");
                        continue;
                    }

                    var outputFileName = fileNamePattern.Replace("[番号]", (i + 1).ToString("D3"));
                    var outputPath = Path.Combine(outputFolder, outputFileName);

                    PdfDocument? outputDocument = null;

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"  [{i + 1}/{ranges.Count}] 作成中: {outputFileName} (ページ {start}-{end})");
                        
                        // 出力用PDFを作成
                        outputDocument = new PdfDocument();

                        // ページをコピー（1-based → 0-basedに変換）
                        for (int pageNum = start; pageNum <= end; pageNum++)
                        {
                            int pageIndex = pageNum - 1; // 0-basedに変換
                            var page = sourceDocument.Pages[pageIndex];
                            outputDocument.AddPage(page);
                        }
                        
                        // 保存してDispose
                        outputDocument.Save(outputPath);
                        outputDocument.Dispose();
                        outputDocument = null; // nullに設定してfinally句での二重Disposeを防ぐ
                        
                        System.Diagnostics.Debug.WriteLine($"    完了: {new FileInfo(outputPath).Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PDF分割エラー [{outputFileName}]: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"  スタックトレース: {ex.StackTrace}");
                        
                        continue;
                    }
                    finally
                    {
                        // まだDispose されていない場合のみDispose
                        try { outputDocument?.Dispose(); } catch { }
                    }

                    var percentage = (int)((i + 1) / (double)ranges.Count * 100);
                    progress?.Report(percentage);
                }

                System.Diagnostics.Debug.WriteLine("PDF分割完了");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF分割エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
                return false;
            }
            finally
            {
                try
                {
                    sourceDocument?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ソースPDF Dispose エラー: {ex.Message}");
                }
            }
        });
    }

    public async Task<bool> SplitByPageAsync(string sourcePath, string outputFolder, 
        string fileNamePattern, IProgress<int>? progress = null)
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

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                // ソースPDFを開く
                sourceDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                int pageCount = sourceDocument.PageCount;
                
                System.Diagnostics.Debug.WriteLine($"PDF分割開始（1ページずつ）: {Path.GetFileName(sourcePath)} ({pageCount}ページ)");

                for (int i = 0; i < pageCount; i++)
                {
                    int pageNumber = i + 1; // 1-basedのページ番号
                    var outputFileName = fileNamePattern.Replace("[番号]", pageNumber.ToString("D3"));
                    var outputPath = Path.Combine(outputFolder, outputFileName);

                    PdfDocument? outputDocument = null;

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"  [{pageNumber}/{pageCount}] 作成中: {outputFileName}");
                        
                        // 出力用PDFを作成
                        outputDocument = new PdfDocument();
                        
                        // 1ページをコピー（0-basedインデックス）
                        var page = sourceDocument.Pages[i];
                        outputDocument.AddPage(page);
                        
                        // 保存してDispose
                        outputDocument.Save(outputPath);
                        outputDocument.Dispose();
                        outputDocument = null; // nullに設定してfinally句での二重Disposeを防ぐ
                        
                        System.Diagnostics.Debug.WriteLine($"    完了: {new FileInfo(outputPath).Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PDF分割エラー (Page {pageNumber}): {ex.Message}");
                        
                        continue;
                    }
                    finally
                    {
                        // まだDispose されていない場合のみDispose
                        try { outputDocument?.Dispose(); } catch { }
                    }

                    var percentage = (int)((pageNumber) / (double)pageCount * 100);
                    progress?.Report(percentage);
                }

                System.Diagnostics.Debug.WriteLine("PDF分割完了");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF分割エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
                return false;
            }
            finally
            {
                try
                {
                    sourceDocument?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ソースPDF Dispose エラー: {ex.Message}");
                }
            }
        });
    }

    public async Task<bool> SplitEquallyAsync(string sourcePath, int parts, string outputFolder, 
        string fileNamePattern, IProgress<int>? progress = null)
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

                if (parts < 1)
                {
                    System.Diagnostics.Debug.WriteLine($"無効な分割数: {parts}");
                    return false;
                }

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                // ソースPDFを開く
                sourceDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                int totalPages = sourceDocument.PageCount;
                int pagesPerPart = (int)Math.Ceiling(totalPages / (double)parts);
                
                System.Diagnostics.Debug.WriteLine($"PDF等分割開始: {Path.GetFileName(sourcePath)} ({totalPages}ページ) → {parts}分割");

                for (int i = 0; i < parts; i++)
                {
                    // 1-basedのページ範囲を計算
                    int startPage = i * pagesPerPart + 1;
                    int endPage = Math.Min((i + 1) * pagesPerPart, totalPages);

                    if (startPage > totalPages)
                        break;

                    var outputFileName = fileNamePattern.Replace("[番号]", (i + 1).ToString("D3"));
                    var outputPath = Path.Combine(outputFolder, outputFileName);

                    PdfDocument? outputDocument = null;

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"  [{i + 1}/{parts}] 作成中: {outputFileName} (ページ {startPage}-{endPage})");
                        
                        // 出力用PDFを作成
                        outputDocument = new PdfDocument();

                        // ページをコピー（1-based → 0-basedに変換）
                        for (int pageNum = startPage; pageNum <= endPage; pageNum++)
                        {
                            int pageIndex = pageNum - 1; // 0-basedに変換
                            var page = sourceDocument.Pages[pageIndex];
                            outputDocument.AddPage(page);
                        }
                        
                        // 保存してDispose
                        outputDocument.Save(outputPath);
                        outputDocument.Dispose();
                        outputDocument = null; // nullに設定してfinally句での二重Disposeを防ぐ
                        
                        System.Diagnostics.Debug.WriteLine($"    完了: {new FileInfo(outputPath).Length} bytes");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"PDF分割エラー (Part {i + 1}): {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"  スタックトレース: {ex.StackTrace}");
                        
                        continue;
                    }
                    finally
                    {
                        // まだDispose されていない場合のみDispose
                        try { outputDocument?.Dispose(); } catch { }
                    }

                    var percentage = (int)((i + 1) / (double)parts * 100);
                    progress?.Report(percentage);
                }

                System.Diagnostics.Debug.WriteLine("PDF等分割完了");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF等分割エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
                return false;
            }
            finally
            {
                try
                {
                    sourceDocument?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ソースPDF Dispose エラー: {ex.Message}");
                }
            }
        });
    }
}
