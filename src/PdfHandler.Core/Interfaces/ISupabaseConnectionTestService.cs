// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

using PdfHandler.Core.Models;

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// 購入・ライセンス認証前の Supabase 接続確認（verify-license は呼ばない）。
/// </summary>
public interface ISupabaseConnectionTestService
{
    /// <summary>
    /// 設定された Supabase ホストへの HTTPS 到達と Edge 経路（ping）を確認する。
    /// </summary>
    Task<SupabaseConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}
