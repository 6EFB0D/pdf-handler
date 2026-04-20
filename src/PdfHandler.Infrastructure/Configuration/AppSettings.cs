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
    /// お問い合わせ先URL（サポート・ボリュームライセンス共通）
    /// リリース前に実際のURLへ差し替えすること。環境変数 CONTACT_URL で上書き可能。
    /// </summary>
    public string ContactUrl { get; set; } = "https://example.com/contact";

    /// <summary>
    /// 商品紹介ページURL（ヘルプ → サポート・お問い合わせ 内で使用）
    /// 環境変数 PRODUCT_PAGE_URL で上書き可能。
    /// </summary>
    public string ProductPageUrl { get; set; } = "https://github.com/6EFB0D/pdf-handler";

    /// <summary>
    /// アンケート・要望フォームURL（Google フォーム等の回答用 /viewform URL）
    /// 環境変数 SURVEY_FORM_URL で上書き可能。
    /// </summary>
    public string SurveyFormUrl { get; set; } = "https://docs.google.com/forms/d/1NpXzk1kyUn2LhUzQhhMHq_tnT1oOGAsv561L-7nMfos/viewform";

    /// <summary>
    /// HMACオフライン検証用の秘密鍵（license-code-specification 準拠）
    /// 環境変数 LICENSE_SECRET_KEY で設定。未設定だとオフライン検証不可。
    /// </summary>
    public string LicenseSecretKey { get; set; } = "";
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



