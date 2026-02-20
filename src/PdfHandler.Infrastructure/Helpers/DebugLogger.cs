// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace PdfHandler.Infrastructure.Helpers;

/// <summary>
/// デバッグログをファイルにも出力するヘルパークラス
/// </summary>
public static class DebugLogger
{
    private static readonly string _logDirectory;
    private static readonly string _logFilePath;
    private static readonly object _lockObject = new object();

    static DebugLogger()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(appDataPath, "PDFHandler", "logs");
        
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }

        var logFileName = $"debug_{DateTime.Now:yyyyMMdd}.log";
        _logFilePath = Path.Combine(_logDirectory, logFileName);
    }

    /// <summary>
    /// デバッグメッセージを出力（Debug.WriteLine + ファイル出力）
    /// </summary>
    public static void WriteLine(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] {message}";

        // Debug.WriteLineに出力（デバッガーがアタッチされている場合に表示）
        System.Diagnostics.Debug.WriteLine(logMessage);

        // ファイルにも出力
        try
        {
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
        }
        catch
        {
            // ファイル出力エラーは無視（デバッグ出力なので）
        }
    }

    /// <summary>
    /// ログファイルのパスを取得
    /// </summary>
    public static string GetLogFilePath()
    {
        return _logFilePath;
    }

    /// <summary>
    /// ログディレクトリのパスを取得
    /// </summary>
    public static string GetLogDirectory()
    {
        return _logDirectory;
    }
}

