// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Infrastructure.Configuration;

/// <summary>
/// アプリケーション設定
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Supabase設定
    /// </summary>
    public SupabaseSettings Supabase { get; set; } = new();

    /// <summary>
    /// Stripe設定
    /// </summary>
    public StripeSettings Stripe { get; set; } = new();

    /// <summary>
    /// Premium版の公開設定（v1.0ではfalse）
    /// </summary>
    public bool EnablePremiumPlan { get; set; } = false;

    /// <summary>
    /// お問い合わせ先URL（サポート・ボリュームライセンス共通）
    /// リリース前に実際のURLへ差し替えすること。環境変数 CONTACT_URL で上書き可能。
    /// </summary>
    public string ContactUrl { get; set; } = "https://example.com/contact";
}

/// <summary>
/// Supabase設定
/// </summary>
public class SupabaseSettings
{
    /// <summary>
    /// Supabase URL
    /// </summary>
    public string Url { get; set; } = "https://yzmjuotvkxcfnsgleyxl.supabase.co";

    /// <summary>
    /// Supabase Anon Key（公開キー）
    /// </summary>
    public string AnonKey { get; set; } = string.Empty;

    /// <summary>
    /// Supabase Service Role Key（サーバーサイド処理用、機密情報）
    /// </summary>
    public string ServiceRoleKey { get; set; } = string.Empty;
}

/// <summary>
/// Stripe設定
/// </summary>
public class StripeSettings
{
    /// <summary>
    /// Stripe公開キー（オプション、Edge Functionで処理する場合は不要）
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripeシークレットキー（サーバーサイド処理用、機密情報）
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
}



