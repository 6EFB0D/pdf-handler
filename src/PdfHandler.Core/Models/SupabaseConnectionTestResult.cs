// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

namespace PdfHandler.Core.Models;

/// <summary>
/// 接続確認の結果カテゴリ（画面上の文言マッピング用。HTTP ステータスは UI に出さない）。
/// </summary>
public enum SupabaseConnectionTestOutcome
{
    Success,
    Misconfigured,
    HostUnreachable,
    TlsError,
    Timeout,
    EdgeUnreachable,
    UnknownError,
}

/// <summary>
/// Supabase への接続確認結果。
/// </summary>
public sealed class SupabaseConnectionTestResult
{
    public bool IsSuccess { get; init; }

    public SupabaseConnectionTestOutcome Outcome { get; init; }

    /// <summary>ユーザー向け日本語メッセージ（HTTP ステータスコードは含めない）。</summary>
    public string UserMessage { get; init; } = string.Empty;

    public bool HostReachable { get; init; }

    public bool EdgeReachable { get; init; }
}
