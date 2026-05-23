// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

namespace PdfHandler.Infrastructure.Configuration;

/// <summary>
/// 接続先 Supabase プロジェクト（DEV / PROD）の識別と表示用ラベル。
/// プロジェクト ref の定数はソースに含めない（PDFHANDLER_ENVIRONMENT または PdfHandler.runtime.json を正とする）。
/// </summary>
public static class AppEnvironmentResolver
{
    public const string Dev = "DEV";
    public const string Prod = "PROD";

    public static void FinalizeTargetEnvironment(AppSettings settings)
    {
        var fromEnv = Environment.GetEnvironmentVariable("PDFHANDLER_ENVIRONMENT");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            settings.TargetEnvironment = NormalizeEnvironmentName(fromEnv);
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.TargetEnvironment))
        {
            settings.TargetEnvironment = NormalizeEnvironmentName(settings.TargetEnvironment);
            return;
        }

        settings.TargetEnvironment = InferFromSupabaseUrl(settings.Supabase.Url);
    }

    public static string InferFromSupabaseUrl(string? supabaseUrl)
    {
        if (string.IsNullOrWhiteSpace(supabaseUrl))
            return Dev;

        return Prod;
    }

    public static string? TryGetProjectRef(string? supabaseUrl)
    {
        var host = TryGetHost(supabaseUrl);
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var dot = host.IndexOf('.');
        return dot > 0 ? host[..dot] : host;
    }

    public static string GetConnectionLabel(AppSettings settings)
    {
        var env = string.IsNullOrWhiteSpace(settings.TargetEnvironment)
            ? InferFromSupabaseUrl(settings.Supabase.Url)
            : settings.TargetEnvironment;
        var projectRef = TryGetProjectRef(settings.Supabase.Url) ?? "不明";
        return $"{env}（Supabase: {projectRef}）";
    }

    public static string GetWindowTitleSuffix(AppSettings settings) =>
        settings.IsDevEnvironment ? " [DEV]" : string.Empty;

    private static string? TryGetHost(string? supabaseUrl)
    {
        if (string.IsNullOrWhiteSpace(supabaseUrl))
            return null;

        try
        {
            return new Uri(supabaseUrl.Trim()).Host;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeEnvironmentName(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized is Dev or Prod ? normalized : Dev;
    }
}
