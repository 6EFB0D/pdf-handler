// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2026 Office Go Plan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace PdfHandler.Infrastructure.Helpers;

/// <summary>
/// アプリケーションログをファイルに出力するヘルパークラス。
/// ログファイル: %LOCALAPPDATA%\PDFHandler\logs\app_YYYYMMDD.log
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
            Directory.CreateDirectory(_logDirectory);

        var logFileName = $"app_{DateTime.Now:yyyyMMdd}.log";
        _logFilePath = Path.Combine(_logDirectory, logFileName);

        // 古いログを削除（30日以上経過したもの）
        CleanupOldLogs(30);
    }

    // ─────────────────────────────────────────
    // 公開ログメソッド
    // ─────────────────────────────────────────

    /// <summary>情報ログ（通常の動作記録）</summary>
    public static void LogInfo(string message)
        => Write("INFO", null, message);

    /// <summary>警告ログ（問題の可能性があるが継続可能）</summary>
    public static void LogWarn(string message, Exception? ex = null)
        => Write("WARN", null, message, ex);

    /// <summary>エラーログ（エラーコード付き）</summary>
    public static void LogError(string errorCode, string message, Exception? ex = null)
        => Write("ERROR", errorCode, message, ex);

    /// <summary>後方互換用（既存コードのDebugLogger.WriteLineを置き換えない場合用）</summary>
    public static void WriteLine(string message)
        => Write("DEBUG", null, message);

    // ─────────────────────────────────────────
    // ユーティリティ
    // ─────────────────────────────────────────

    /// <summary>ログファイルのパスを取得</summary>
    public static string GetLogFilePath() => _logFilePath;

    /// <summary>ログディレクトリのパスを取得</summary>
    public static string GetLogDirectory() => _logDirectory;

    // ─────────────────────────────────────────
    // 内部実装
    // ─────────────────────────────────────────

    private static void Write(string level, string? errorCode, string message, Exception? ex = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var codeTag = errorCode != null ? $"[{errorCode}] " : "";
        var header = $"[{timestamp}] [{level}] {codeTag}{message}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(header);

        if (ex != null)
        {
            sb.AppendLine($"  Exception : {ex.GetType().FullName}");
            sb.AppendLine($"  Message   : {ex.Message}");
            if (ex.InnerException != null)
                sb.AppendLine($"  InnerEx   : {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            sb.AppendLine($"  StackTrace: {ex.StackTrace}");
        }

        var logEntry = sb.ToString();

        // デバッガーへの出力
        System.Diagnostics.Debug.Write(logEntry);

        // ファイル出力
        try
        {
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, logEntry);
            }
        }
        catch
        {
            // ログ出力自体の失敗は無視
        }
    }

    private static void CleanupOldLogs(int retentionDays)
    {
        try
        {
            if (!Directory.Exists(_logDirectory)) return;
            var cutoff = DateTime.Now.AddDays(-retentionDays);
            foreach (var file in Directory.GetFiles(_logDirectory, "app_*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch
        {
            // ログクリーンアップ失敗は無視
        }
    }
}
