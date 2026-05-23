// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.Infrastructure.Configuration;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// Supabase への HTTPS 到達確認（auth health）と Edge 経路（ping）のテスト。
/// ライセンス検証 API は呼び出さない。
/// </summary>
public sealed class SupabaseConnectionTestService : ISupabaseConnectionTestService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    private readonly AppSettings _settings;

    public SupabaseConnectionTestService(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<SupabaseConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = NormalizeBaseUrl(_settings.Supabase.Url);
        if (string.IsNullOrEmpty(baseUrl))
        {
            return Result(
                SupabaseConnectionTestOutcome.Misconfigured,
                "接続先の設定が正しくありません。アプリを再インストールするか、サポートにお問い合わせください。",
                hostReachable: false,
                edgeReachable: false);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        try
        {
            using var client = CreateHttpClient();

            // 購入・認証と同じ Edge 経路を先に確認（verify-license / request-checkout と同等）
            var edgeOk = await TryPingEdgeAsync(client, baseUrl, timeoutCts.Token).ConfigureAwait(false);
            if (edgeOk)
            {
                return BuildSuccessResult();
            }

            // フォールバック: ホスト到達のみ（auth health は匿名だと 401 になることがある）
            var healthUrl = $"{baseUrl}/auth/v1/health";
            using var healthResponse = await client.GetAsync(healthUrl, timeoutCts.Token).ConfigureAwait(false);

            if (IsHostReachable(healthResponse))
            {
                return Result(
                    SupabaseConnectionTestOutcome.EdgeUnreachable,
                    "ライセンスサーバーには接続できましたが、購入・認証の通信経路を確認できませんでした。しばらくしてから再度お試しください。問題が続く場合はサポートにお問い合わせください。",
                    hostReachable: true,
                    edgeReachable: false);
            }

            return Result(
                SupabaseConnectionTestOutcome.HostUnreachable,
                "ライセンスサーバーに接続できませんでした。インターネット接続と、社内のファイアウォール・プロキシ設定をご確認ください。",
                hostReachable: false,
                edgeReachable: false);

        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result(
                SupabaseConnectionTestOutcome.Timeout,
                "サーバーへの接続がタイムアウトしました。ネットワークが遅い、またはファイアウォールで通信が遮断されている可能性があります。",
                hostReachable: false,
                edgeReachable: false);
        }
        catch (Exception ex)
        {
            return MapException(ex);
        }
    }

    private async Task<bool> TryPingEdgeAsync(HttpClient client, string baseUrl, CancellationToken cancellationToken)
    {
        var anonKey = _settings.Supabase.AnonKey?.Trim();
        if (string.IsNullOrEmpty(anonKey))
            return false;

        var pingUrl = $"{baseUrl}/functions/v1/ping";
        using var request = new HttpRequestMessage(HttpMethod.Get, pingUrl);
        request.Headers.TryAddWithoutValidation("apikey", anonKey);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {anonKey}");

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return false;

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private SupabaseConnectionTestResult BuildSuccessResult()
    {
        var envNote = _settings.IsDevEnvironment
            ? "\n（現在は開発用サーバーに接続しています。）"
            : string.Empty;

        return Result(
            SupabaseConnectionTestOutcome.Success,
            "サーバーへの接続を確認できました。購入手続きやライセンス認証に必要な通信が行える状態です。" + envNote,
            hostReachable: true,
            edgeReachable: true,
            isSuccess: true);
    }

    /// <summary>
    /// TLS まで到達し Supabase が応答したか（401 等の 4xx は到達扱い。5xx は不可）。
    /// </summary>
    private static bool IsHostReachable(HttpResponseMessage response)
    {
        var code = (int)response.StatusCode;
        return code is > 0 and < 500;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = RequestTimeout };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PDFHandler");
        return client;
    }

    private static string? NormalizeBaseUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static SupabaseConnectionTestResult MapException(Exception ex)
    {
        if (ContainsSslIssue(ex))
        {
            return Result(
                SupabaseConnectionTestOutcome.TlsError,
                "安全な通信（HTTPS）を確立できませんでした。社内プロキシで SSL を検査している場合は、例外設定が必要なことがあります。",
                hostReachable: false,
                edgeReachable: false);
        }

        if (ContainsDnsOrSocketIssue(ex))
        {
            return Result(
                SupabaseConnectionTestOutcome.HostUnreachable,
                "ライセンスサーバーのホスト名を解決できませんでした。DNS またはネットワーク設定をご確認ください。",
                hostReachable: false,
                edgeReachable: false);
        }

        return Result(
            SupabaseConnectionTestOutcome.UnknownError,
            "接続確認中に問題が発生しました。インターネット接続をご確認のうえ、再度お試しください。",
            hostReachable: false,
            edgeReachable: false);
    }

    private static bool ContainsSslIssue(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is AuthenticationException)
                return true;
        }

        return false;
    }

    private static bool ContainsDnsOrSocketIssue(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is SocketException)
                return true;
        }

        return false;
    }

    private static SupabaseConnectionTestResult Result(
        SupabaseConnectionTestOutcome outcome,
        string userMessage,
        bool hostReachable,
        bool edgeReachable,
        bool isSuccess = false)
    {
        return new SupabaseConnectionTestResult
        {
            IsSuccess = isSuccess,
            Outcome = outcome,
            UserMessage = userMessage,
            HostReachable = hostReachable,
            EdgeReachable = edgeReachable,
        };
    }
}
