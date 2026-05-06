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
/// PDF結合サービスの実装（PdfSharp版 - デバッグ強化版）
/// </summary>
public class PdfMergeService : IPdfMergeService
{
    public async Task<bool> MergePdfsAsync(List<string> sourcePaths, string outputPath, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            PdfDocument? outputDocument = null;
            var inputDocuments = new List<PdfDocument>();

            try
            {
                if (sourcePaths == null || sourcePaths.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("PDF結合エラー: ソースファイルが指定されていません");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"=== PDF結合開始 ===");
                System.Diagnostics.Debug.WriteLine($"ファイル数: {sourcePaths.Count}");
                System.Diagnostics.Debug.WriteLine($"出力先: {outputPath}");

                // 出力先のディレクトリが存在しない場合は作成
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    System.Diagnostics.Debug.WriteLine($"出力ディレクトリを作成: {outputDir}");
                    Directory.CreateDirectory(outputDir);
                }

                // 出力用PDFドキュメントを作成
                System.Diagnostics.Debug.WriteLine("出力PDFドキュメントを作成");
                outputDocument = new PdfDocument();

                for (int i = 0; i < sourcePaths.Count; i++)
                {
                    var sourcePath = sourcePaths[i];
                    
                    if (!File.Exists(sourcePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[警告] ファイルが見つかりません: {sourcePath}");
                        continue;
                    }

                    PdfDocument? inputDocument = null;

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"--- [{i + 1}/{sourcePaths.Count}] {Path.GetFileName(sourcePath)} ---");
                        
                        // ソースPDFを開く（Importモード）
                        System.Diagnostics.Debug.WriteLine("  PDFを開いています...");
                        inputDocument = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
                        int pageCount = inputDocument.PageCount;
                        System.Diagnostics.Debug.WriteLine($"  ページ数: {pageCount}");
                        
                        // 全ページをコピー
                        System.Diagnostics.Debug.WriteLine($"  ページコピー開始...");
                        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                        {
                            var page = inputDocument.Pages[pageIndex];
                            outputDocument.AddPage(page);
                            
                            if ((pageIndex + 1) % 10 == 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"    {pageIndex + 1}/{pageCount} ページコピー済み");
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"  ✅ コピー完了");
                        
                        // 入力ドキュメントをリストに追加（まだDisposeしない）
                        inputDocuments.Add(inputDocument);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[エラー] PDF読み込み失敗: {Path.GetFileName(sourcePath)}");
                        System.Diagnostics.Debug.WriteLine($"  エラー内容: {ex.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"  メッセージ: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"  内部エラー: {ex.InnerException.Message}");
                        }
                        
                        // エラーが出た場合はこのドキュメントだけDispose
                        try { inputDocument?.Dispose(); } catch { }
                        
                        continue;
                    }

                    // 進捗報告
                    var percentage = (int)((i + 1) / (double)sourcePaths.Count * 100);
                    progress?.Report(percentage);
                }

                // ページが1つも追加されていない場合
                if (outputDocument.PageCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[エラー] 結合できるページがありませんでした");
                    return false;
                }

                // 出力PDFを保存
                System.Diagnostics.Debug.WriteLine($"=== 保存処理開始 ===");
                System.Diagnostics.Debug.WriteLine($"総ページ数: {outputDocument.PageCount}");
                System.Diagnostics.Debug.WriteLine($"保存先: {outputPath}");
                
                try
                {
                    outputDocument.Save(outputPath);
                    System.Diagnostics.Debug.WriteLine("✅ Save() 成功");
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[エラー] Save() 失敗");
                    System.Diagnostics.Debug.WriteLine($"  エラー内容: {saveEx.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"  メッセージ: {saveEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"  スタックトレース: {saveEx.StackTrace}");
                    throw; // 再スロー
                }
                
                // 保存成功後、入力PDFをDispose
                System.Diagnostics.Debug.WriteLine("入力PDFをDispose中...");
                foreach (var doc in inputDocuments)
                {
                    try
                    {
                        doc.Dispose();
                    }
                    catch (Exception disposeEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[警告] 入力PDF Dispose エラー: {disposeEx.Message}");
                    }
                }
                inputDocuments.Clear();
                
                // 出力PDFをDispose
                System.Diagnostics.Debug.WriteLine("出力PDFをDispose中...");
                try
                {
                    outputDocument.Dispose();
                    outputDocument = null;
                    System.Diagnostics.Debug.WriteLine("✅ Dispose 成功");
                }
                catch (Exception disposeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[警告] 出力PDF Dispose エラー: {disposeEx.Message}");
                }
                
                // ファイルが実際に作成されたか確認
                if (File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    System.Diagnostics.Debug.WriteLine($"✅ ファイル作成確認: {fileInfo.Length} bytes");
                    System.Diagnostics.Debug.WriteLine("=== PDF結合完了 ===");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[エラー] ファイルが作成されていません");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 予期しないエラー ===");
                System.Diagnostics.Debug.WriteLine($"エラー内容: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"メッセージ: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"スタックトレース:");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"内部エラー: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"内部スタックトレース:");
                    System.Diagnostics.Debug.WriteLine(ex.InnerException.StackTrace);
                }
                return false;
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("=== finally句実行 ===");
                
                // まだDispose されていない入力PDFをDispose
                if (inputDocuments.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"残っている入力PDF数: {inputDocuments.Count}");
                    foreach (var doc in inputDocuments)
                    {
                        try
                        {
                            doc.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[finally] 入力PDF Dispose エラー: {ex.Message}");
                        }
                    }
                }
                
                // まだDispose されていない出力PDFをDispose
                if (outputDocument != null)
                {
                    System.Diagnostics.Debug.WriteLine("出力PDFがまだDispose されていません");
                    try
                    {
                        outputDocument.Dispose();
                        System.Diagnostics.Debug.WriteLine("[finally] 出力PDF Dispose 成功");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[finally] 出力PDF Dispose エラー: {ex.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("=== finally句完了 ===");
            }
        });
    }
}
