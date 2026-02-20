// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using PdfHandler.Core.Interfaces;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// PDF回転サービスの実装
/// </summary>
public class PdfRotateService : IPdfRotateService
{
    /// <summary>
    /// PDFの指定ページを回転
    /// </summary>
    public async Task<bool> RotatePageAsync(string filePath, int pageNumber, int rotationDegrees, string? outputPath = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"PDF回転エラー: ファイルが見つかりません: {filePath}");
                    return false;
                }

                // 回転角度を検証（90, 180, 270のみ）
                if (rotationDegrees != 90 && rotationDegrees != 180 && rotationDegrees != 270)
                {
                    System.Diagnostics.Debug.WriteLine($"PDF回転エラー: 無効な回転角度: {rotationDegrees}");
                    return false;
                }

                // ページ番号を検証（1ベース）
                if (pageNumber < 1)
                {
                    System.Diagnostics.Debug.WriteLine($"PDF回転エラー: 無効なページ番号: {pageNumber}");
                    return false;
                }

                var finalOutputPath = outputPath ?? filePath;
                var outputDir = Path.GetDirectoryName(finalOutputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // PDFを読み込み
                byte[] fileBytes = File.ReadAllBytes(filePath);
                using var inputDocument = PdfReader.Open(new MemoryStream(fileBytes), PdfDocumentOpenMode.Modify);
                
                // ページ番号を0ベースに変換
                int pageIndex = pageNumber - 1;
                
                if (pageIndex < 0 || pageIndex >= inputDocument.PageCount)
                {
                    System.Diagnostics.Debug.WriteLine($"PDF回転エラー: ページ番号が範囲外: {pageNumber} (総ページ数: {inputDocument.PageCount})");
                    return false;
                }

                // 現在の回転角度を取得
                var page = inputDocument.Pages[pageIndex];
                var currentRotation = page.Rotate;
                
                // 新しい回転角度を設定（現在の角度に加算）
                var newRotation = (currentRotation + rotationDegrees) % 360;
                page.Rotate = newRotation;

                // 保存
                if (finalOutputPath == filePath)
                {
                    // 上書きの場合は一時ファイルを使用
                    var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                    inputDocument.Save(tempPath);
                    
                    // ファイルがロックされていないことを確認してからコピー
                    int retryCount = 0;
                    const int maxRetries = 10;
                    bool copied = false;
                    
                    while (retryCount < maxRetries && !copied)
                    {
                        try
                        {
                            // ファイルをコピー（直接試行）
                            File.Copy(tempPath, filePath, true);
                            copied = true;
                        }
                        catch (IOException ioEx)
                        {
                            // ファイルがロックされている場合は少し待ってからリトライ
                            retryCount++;
                            if (retryCount < maxRetries)
                            {
                                System.Threading.Thread.Sleep(200);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"PDF回転エラー: ファイルがロックされています: {filePath}, エラー: {ioEx.Message}");
                            }
                        }
                        catch (UnauthorizedAccessException uaEx)
                        {
                            // アクセス権限エラー
                            System.Diagnostics.Debug.WriteLine($"PDF回転エラー: アクセス権限がありません: {filePath}, エラー: {uaEx.Message}");
                            break;
                        }
                    }
                    
                    // 一時ファイルを削除
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // 一時ファイルの削除に失敗しても続行
                    }
                    
                    if (!copied)
                    {
                        System.Diagnostics.Debug.WriteLine($"PDF回転エラー: ファイルがロックされています: {filePath}");
                        return false;
                    }
                }
                else
                {
                    inputDocument.Save(finalOutputPath);
                }

                System.Diagnostics.Debug.WriteLine($"PDF回転成功: ページ {pageNumber} を {rotationDegrees}度回転");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF回転エラー: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// PDFの全ページを回転
    /// </summary>
    public async Task<bool> RotateAllPagesAsync(string filePath, int rotationDegrees, string? outputPath = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"PDF回転エラー: ファイルが見つかりません: {filePath}");
                    return false;
                }

                // 回転角度を検証
                if (rotationDegrees != 90 && rotationDegrees != 180 && rotationDegrees != 270)
                {
                    System.Diagnostics.Debug.WriteLine($"PDF回転エラー: 無効な回転角度: {rotationDegrees}");
                    return false;
                }

                var finalOutputPath = outputPath ?? filePath;
                var outputDir = Path.GetDirectoryName(finalOutputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // PDFを読み込み
                byte[] fileBytes = File.ReadAllBytes(filePath);
                PdfDocument? inputDocument = null;
                
                try
                {
                    using (var stream = new MemoryStream(fileBytes))
                    {
                        inputDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Modify);
                    }

                    // 全ページを回転
                    for (int i = 0; i < inputDocument.PageCount; i++)
                    {
                        var page = inputDocument.Pages[i];
                        var currentRotation = page.Rotate;
                        var newRotation = (currentRotation + rotationDegrees) % 360;
                        page.Rotate = newRotation;
                    }

                    // 保存（RotatePageAsyncと同じ実装を使用）
                    if (finalOutputPath == filePath)
                    {
                        // 上書きの場合は一時ファイルを使用
                        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                        inputDocument.Save(tempPath);
                        
                        // Save後に明示的に閉じる（PdfSharpの「既に保存済み」エラーを回避）
                        inputDocument.Close();
                        inputDocument.Dispose();
                        inputDocument = null;
                        
                        // ファイルがロックされていないことを確認してからコピー
                        int retryCount = 0;
                        const int maxRetries = 10;
                        bool copied = false;
                        
                        while (retryCount < maxRetries && !copied)
                        {
                            try
                            {
                                // ファイルをコピー（直接試行）
                                File.Copy(tempPath, filePath, true);
                                copied = true;
                            }
                            catch (IOException ioEx)
                            {
                                // ファイルがロックされている場合は少し待ってからリトライ
                                retryCount++;
                                System.Diagnostics.Debug.WriteLine($"PDF回転: リトライ {retryCount}/{maxRetries} - {ioEx.Message}");
                                if (retryCount < maxRetries)
                                {
                                    System.Threading.Thread.Sleep(200);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"PDF回転エラー: ファイルがロックされています: {filePath}, エラー: {ioEx.Message}");
                                }
                            }
                            catch (UnauthorizedAccessException uaEx)
                            {
                                // アクセス権限エラー
                                System.Diagnostics.Debug.WriteLine($"PDF回転エラー: アクセス権限がありません: {filePath}, エラー: {uaEx.Message}");
                                break;
                            }
                        }
                        
                        // 一時ファイルを削除
                        try
                        {
                            File.Delete(tempPath);
                        }
                        catch
                        {
                            // 一時ファイルの削除に失敗しても続行
                        }
                        
                        if (!copied)
                        {
                            System.Diagnostics.Debug.WriteLine($"PDF回転エラー: ファイルがロックされています: {filePath}");
                            return false;
                        }
                    }
                    else
                    {
                        inputDocument.Save(finalOutputPath);
                        inputDocument.Close();
                        inputDocument.Dispose();
                        inputDocument = null;
                    }

                    System.Diagnostics.Debug.WriteLine($"PDF回転成功: 全ページを{rotationDegrees}度回転");
                    return true;
                }
                finally
                {
                    // ドキュメントがまだ開いている場合は閉じる
                    if (inputDocument != null)
                    {
                        try
                        {
                            inputDocument.Close();
                            inputDocument.Dispose();
                        }
                        catch
                        {
                            // 既に閉じられている場合は無視
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorDetails = $"PDF回転エラー: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorDetails += $"\n内部例外: {ex.InnerException.Message}";
                }
                errorDetails += $"\nスタックトレース: {ex.StackTrace}";
                
                System.Diagnostics.Debug.WriteLine(errorDetails);
                System.Console.WriteLine(errorDetails); // コンソールにも出力
                
                // 例外を再スローして、呼び出し元で詳細を確認できるようにする
                throw new Exception($"PDF回転に失敗しました: {ex.Message}", ex);
            }
        });
    }
}


