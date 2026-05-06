// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using PdfHandler.Core.Interfaces;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// 専用ワークフォルダサービスの実装
/// </summary>
public class WorkFolderService : IWorkFolderService
{
    private readonly string _settingsFilePath;
    private string? _workFolderPath;

    public WorkFolderService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pdfHandlerPath = Path.Combine(appDataPath, "PDFHandler");
        
        if (!Directory.Exists(pdfHandlerPath))
        {
            Directory.CreateDirectory(pdfHandlerPath);
        }

        _settingsFilePath = Path.Combine(pdfHandlerPath, "settings.json");
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
                var settings = System.Text.Json.JsonSerializer.Deserialize<WorkFolderSettings>(json);
                if (settings != null && !string.IsNullOrEmpty(settings.WorkFolderPath))
                {
                    _workFolderPath = settings.WorkFolderPath;
                }
            }
        }
        catch
        {
            // エラー時はデフォルト値を使用
        }

        // デフォルトパスが設定されていない場合は設定
        if (string.IsNullOrEmpty(_workFolderPath))
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _workFolderPath = Path.Combine(documentsPath, "PDFHandler", "Work");
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
                var settings = new WorkFolderSettings
                {
                    WorkFolderPath = _workFolderPath ?? string.Empty
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
    /// ワークフォルダのパスを取得
    /// </summary>
    public string GetWorkFolderPath()
    {
        return _workFolderPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PDFHandler",
            "Work");
    }

    /// <summary>
    /// ワークフォルダのパスを設定
    /// </summary>
    public async Task SetWorkFolderPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        _workFolderPath = path;
        await SaveSettingsAsync();
    }

    /// <summary>
    /// ワークフォルダが存在するかどうかを確認
    /// </summary>
    public bool WorkFolderExists()
    {
        var path = GetWorkFolderPath();
        return Directory.Exists(path);
    }

    /// <summary>
    /// ワークフォルダを作成
    /// </summary>
    public async Task<bool> CreateWorkFolderAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var path = GetWorkFolderPath();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
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
    /// ワークフォルダにファイルを保存
    /// </summary>
    public async Task<string> SaveToWorkFolderAsync(string sourceFilePath, string? fileName = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                // ワークフォルダが存在しない場合は作成
                if (!WorkFolderExists())
                {
                    CreateWorkFolderAsync().Wait();
                }

                var workFolderPath = GetWorkFolderPath();
                var targetFileName = fileName ?? Path.GetFileName(sourceFilePath);
                var targetPath = Path.Combine(workFolderPath, targetFileName);

                // 同名ファイルが存在する場合は番号を付加
                int counter = 1;
                var baseName = Path.GetFileNameWithoutExtension(targetFileName);
                var extension = Path.GetExtension(targetFileName);
                while (File.Exists(targetPath))
                {
                    targetFileName = $"{baseName}_{counter}{extension}";
                    targetPath = Path.Combine(workFolderPath, targetFileName);
                    counter++;
                }

                File.Copy(sourceFilePath, targetPath, false);
                return targetPath;
            }
            catch
            {
                return string.Empty;
            }
        });
    }

    /// <summary>
    /// ワークフォルダ設定
    /// </summary>
    private class WorkFolderSettings
    {
        public string WorkFolderPath { get; set; } = string.Empty;
    }
}






