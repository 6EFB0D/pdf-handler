// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using System.Text.Json;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// PDFバインダーサービスの実装
/// </summary>
public class BinderService : IBinderService
{
    private readonly string _bindersFolderPath;
    private readonly IPdfMergeService _pdfMergeService;

    public BinderService(IPdfMergeService pdfMergeService)
    {
        _pdfMergeService = pdfMergeService;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pdfHandlerPath = Path.Combine(appDataPath, "PDFHandler", "Binders");
        
        if (!Directory.Exists(pdfHandlerPath))
        {
            Directory.CreateDirectory(pdfHandlerPath);
        }

        _bindersFolderPath = pdfHandlerPath;
    }

    /// <summary>
    /// バインダーファイルのパスを取得
    /// </summary>
    private string GetBinderFilePath(string binderId)
    {
        return Path.Combine(_bindersFolderPath, $"{binderId}.json");
    }

    /// <summary>
    /// すべてのバインダーを取得
    /// </summary>
    public async Task<List<Binder>> GetAllBindersAsync()
    {
        return await Task.Run(() =>
        {
            var binders = new List<Binder>();
            
            try
            {
                if (!Directory.Exists(_bindersFolderPath))
                    return binders;

                var files = Directory.GetFiles(_bindersFolderPath, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var binder = JsonSerializer.Deserialize<Binder>(json);
                        if (binder != null)
                        {
                            binders.Add(binder);
                        }
                    }
                    catch
                    {
                        // 読み込みエラーは無視
                    }
                }
            }
            catch
            {
                // エラー時は空のリストを返す
            }

            return binders.OrderByDescending(b => b.UpdatedDate).ToList();
        });
    }

    /// <summary>
    /// バインダーを取得
    /// </summary>
    public async Task<Binder?> GetBinderAsync(string binderId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var filePath = GetBinderFilePath(binderId);
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Binder>(json);
            }
            catch
            {
                return null;
            }
        });
    }

    /// <summary>
    /// バインダーを作成
    /// </summary>
    public async Task<Binder> CreateBinderAsync(string name, string? description = null)
    {
        return await Task.Run(() =>
        {
            var binder = new Binder
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            SaveBinder(binder);
            return binder;
        });
    }

    /// <summary>
    /// バインダーを更新
    /// </summary>
    public async Task<bool> UpdateBinderAsync(Binder binder)
    {
        return await Task.Run(() =>
        {
            try
            {
                binder.UpdatedDate = DateTime.Now;
                SaveBinder(binder);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// バインダーを削除
    /// </summary>
    public async Task<bool> DeleteBinderAsync(string binderId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var filePath = GetBinderFilePath(binderId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// バインダーにPDFファイルを追加
    /// </summary>
    public async Task<bool> AddPdfToBinderAsync(string binderId, string pdfFilePath)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var binder = await GetBinderAsync(binderId);
                if (binder == null)
                    return false;

                if (!binder.PdfFilePaths.Contains(pdfFilePath))
                {
                    binder.PdfFilePaths.Add(pdfFilePath);
                    await UpdateBinderAsync(binder);
                }
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// バインダーからPDFファイルを削除
    /// </summary>
    public async Task<bool> RemovePdfFromBinderAsync(string binderId, string pdfFilePath)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var binder = await GetBinderAsync(binderId);
                if (binder == null)
                    return false;

                binder.PdfFilePaths.Remove(pdfFilePath);
                await UpdateBinderAsync(binder);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// バインダー内のPDFファイルの順序を変更
    /// </summary>
    public async Task<bool> ReorderPdfFilesAsync(string binderId, List<string> orderedFilePaths)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var binder = await GetBinderAsync(binderId);
                if (binder == null)
                    return false;

                binder.PdfFilePaths = orderedFilePaths;
                await UpdateBinderAsync(binder);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// バインダーをPDFファイルに結合
    /// </summary>
    public async Task<bool> MergeBinderToPdfAsync(string binderId, string outputPath, IProgress<int>? progress = null)
    {
        try
        {
            var binder = await GetBinderAsync(binderId);
            if (binder == null || binder.PdfFilePaths.Count == 0)
                return false;

            // 存在するファイルのみをフィルタ
            var existingFiles = binder.PdfFilePaths.Where(File.Exists).ToList();
            if (existingFiles.Count == 0)
                return false;

            return await _pdfMergeService.MergePdfsAsync(existingFiles, outputPath, progress);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// バインダーを保存
    /// </summary>
    private void SaveBinder(Binder binder)
    {
        try
        {
            var filePath = GetBinderFilePath(binder.Id);
            var json = JsonSerializer.Serialize(binder, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // 保存エラーは無視
        }
    }
}






