// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using PdfHandler.Core.Interfaces;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// プリンタドライバサービスの実装
/// Microsoft Print to PDFを使用せず、PDFを直接コピーして専用ワークフォルダに保存
/// </summary>
public class PrintToPdfService : IPrintToPdfService
{
    private readonly IWorkFolderService _workFolderService;
    private readonly string _settingsFilePath;
    private IPrintToPdfService.FileNamePattern _fileNamePattern = IPrintToPdfService.FileNamePattern.OriginalFileName;

    public PrintToPdfService(IWorkFolderService workFolderService)
    {
        _workFolderService = workFolderService;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pdfHandlerPath = Path.Combine(appDataPath, "PDFHandler");
        _settingsFilePath = Path.Combine(pdfHandlerPath, "print_settings.json");
        LoadSettings();
    }

    /// <summary>
    /// 設定を読み込み
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<PrintSettings>(json);
                if (settings != null)
                {
                    _fileNamePattern = settings.FileNamePattern;
                }
            }
        }
        catch
        {
            // エラー時はデフォルト値を使用
        }
    }

    /// <summary>
    /// 設定を保存
    /// </summary>
    private async Task SaveSettingsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var settings = new PrintSettings
                {
                    FileNamePattern = _fileNamePattern
                };
                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // 保存エラーは無視
            }
        });
    }

    /// <summary>
    /// ファイル名パターンを設定
    /// </summary>
    public async Task SetFileNamePatternAsync(IPrintToPdfService.FileNamePattern pattern)
    {
        _fileNamePattern = pattern;
        await SaveSettingsAsync();
    }

    /// <summary>
    /// 現在のファイル名パターンを取得
    /// </summary>
    public IPrintToPdfService.FileNamePattern GetFileNamePattern()
    {
        return _fileNamePattern;
    }

    /// <summary>
    /// ファイル名を生成
    /// </summary>
    private string GenerateFileName(string sourceFilePath)
    {
        return _fileNamePattern switch
        {
            IPrintToPdfService.FileNamePattern.Timestamp => $"{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            _ => Path.GetFileName(sourceFilePath)
        };
    }

    /// <summary>
    /// PDFファイルを印刷（専用ワークフォルダに保存）
    /// </summary>
    public async Task<bool> PrintToPdfAsync(string sourceFilePath)
    {
        return await Task.Run(async () =>
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"PrintToPdfエラー: ファイルが見つかりません: {sourceFilePath}");
                    return false;
                }

                // ワークフォルダが存在しない場合は作成
                if (!_workFolderService.WorkFolderExists())
                {
                    await _workFolderService.CreateWorkFolderAsync();
                }

                var fileName = GenerateFileName(sourceFilePath);
                var workFolderPath = _workFolderService.GetWorkFolderPath();
                var targetPath = Path.Combine(workFolderPath, fileName);

                // 同名ファイルが存在する場合は番号を付加
                int counter = 1;
                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                while (File.Exists(targetPath))
                {
                    fileName = $"{baseName}_{counter}{extension}";
                    targetPath = Path.Combine(workFolderPath, fileName);
                    counter++;
                }

                // PDFファイルをコピー
                File.Copy(sourceFilePath, targetPath, false);
                System.Diagnostics.Debug.WriteLine($"PrintToPdf成功: {sourceFilePath} -> {targetPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PrintToPdfエラー: {ex.Message}");
                return false;
            }
        });
    }

    /// <summary>
    /// 複数のPDFファイルを印刷（専用ワークフォルダに保存）
    /// </summary>
    public async Task<bool> PrintMultipleToPdfAsync(List<string> sourceFilePaths, IProgress<int>? progress = null)
    {
        return await Task.Run(async () =>
        {
            try
            {
                int total = sourceFilePaths.Count;
                int completed = 0;

                foreach (var filePath in sourceFilePaths)
                {
                    await PrintToPdfAsync(filePath);
                    completed++;
                    progress?.Report((completed * 100) / total);
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
    /// 印刷設定
    /// </summary>
    private class PrintSettings
    {
        public IPrintToPdfService.FileNamePattern FileNamePattern { get; set; } = IPrintToPdfService.FileNamePattern.OriginalFileName;
    }
}






