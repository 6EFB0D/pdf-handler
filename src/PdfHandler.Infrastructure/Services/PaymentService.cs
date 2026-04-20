// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.Infrastructure.Configuration;
using PdfHandler.Infrastructure.Helpers;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// 決済サービスの実装（Stripe統合）
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    public PaymentService(AppSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("apikey", _settings.Supabase.AnonKey);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.Supabase.AnonKey}");
    }

    /// <summary>
    /// 購入手続きメールの送信を要求する（買い切り Standard のみ）
    /// </summary>
    /// <remarks>
    /// request-checkout Edge Function を呼び出し、サーバ側で Stripe Checkout Session 作成 &amp; 決済リンク入りメール送信を行う。
    /// 旧 CreateCheckoutSessionAsync と異なり、Checkout URL はクライアントには返されない。
    /// </remarks>
    public async Task<RequestCheckoutResult> RequestCheckoutAsync(LicensePlan plan, string customerEmail)
    {
        try
        {
            if (plan != LicensePlan.StandardPurchased)
            {
                throw new InvalidOperationException("新規販売は Standard版（買い切り）のみです。");
            }

            var email = customerEmail?.Trim() ?? "";
            if (string.IsNullOrEmpty(email))
            {
                throw new InvalidOperationException("メールアドレスを入力してください。");
            }

            try
            {
                _ = new MailAddress(email);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("メールアドレスの形式が正しくありません。");
            }

            // Dictionary でキーを固定（匿名型や命名ポリシー差で Edge が読み取れないのを防ぐ）
            var request = new Dictionary<string, string>
            {
                ["plan"] = plan.ToString(),
                ["appId"] = "PDFH",
                ["customerEmail"] = email,
                ["majorVersion"] = "1",
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUrl = $"{_settings.Supabase.Url}/functions/v1/request-checkout";
            var maskedEmail = MaskEmail(email);

            DebugLogger.WriteLine($"=== 購入手続きメール送信リクエスト開始 ===");
            DebugLogger.WriteLine($"リクエストURL: {requestUrl}");
            DebugLogger.WriteLine($"リクエスト: plan={plan}, appId=PDFH, customerEmail={maskedEmail}");

            var response = await _httpClient.PostAsync(requestUrl, content);

            DebugLogger.WriteLine($"レスポンスステータス: {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStringAsync();
            DebugLogger.WriteLine($"レスポンス内容: {responseContent}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (!response.IsSuccessStatusCode)
            {
                // サーバエラーの詳細はログにのみ残し、ユーザーにはシンプルなメッセージを表示
                var errInfo = TryParseError(responseContent, options);
                var internalDetail = errInfo.Detail ?? errInfo.Error ?? responseContent;
                DebugLogger.WriteLine($"サーバエラー詳細: {internalDetail}");
                throw new Exception("購入手続きメールの送信に失敗しました。しばらくしてから再度お試しください。");
            }

            var dto = JsonSerializer.Deserialize<RequestCheckoutResponseDto>(responseContent, options);
            if (dto == null || !dto.Success)
            {
                DebugLogger.WriteLine($"success=false または解析失敗。レスポンス: {responseContent}");
                throw new Exception("購入手続きメールの送信に失敗しました。");
            }

            DebugLogger.WriteLine($"メール送信成功: emailMasked={dto.EmailMasked}");

            return new RequestCheckoutResult
            {
                Success = true,
                EmailMasked = string.IsNullOrWhiteSpace(dto.EmailMasked) ? maskedEmail : dto.EmailMasked,
                Message = string.IsNullOrWhiteSpace(dto.Message)
                    ? "お支払い用のメールを送信しました。"
                    : dto.Message,
            };
        }
        catch (InvalidOperationException)
        {
            // 入力バリデーションエラーはそのまま呼び出し元へ（ユーザー向けメッセージのまま）
            throw;
        }
        catch (Exception ex)
        {
            DebugLogger.WriteLine($"購入手続きメール送信エラー: {ex.GetType().Name} - {ex.Message}");
            DebugLogger.WriteLine($"スタックトレース: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                DebugLogger.WriteLine($"内部例外: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? $"{email[0]}***{email.Substring(at)}" : "***";
    }

    private static (string? Error, string? Detail) TryParseError(string body, JsonSerializerOptions options)
    {
        try
        {
            var err = JsonSerializer.Deserialize<ErrorResponseDto>(body, options);
            return (err?.Error, err?.Detail);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// ライセンスキーを検証
    /// </summary>
    public async Task<bool> VerifyLicenseKeyAsync(string licenseKey)
    {
        try
        {
            var request = new { licenseKey };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_settings.Supabase.Url}/functions/v1/verify-license",
                content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LicenseVerificationResponse>(responseJson);
                return result?.IsValid ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            DebugLogger.WriteLine($"ライセンスキー検証エラー: {ex.GetType().Name} - {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// request-checkout Edge Function の成功レスポンス（内部 DTO）
/// </summary>
internal class RequestCheckoutResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("emailMasked")]
    public string EmailMasked { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Edge Function のエラーレスポンス（内部 DTO）
/// </summary>
internal class ErrorResponseDto
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

/// <summary>
/// ライセンスキー検証レスポンス
/// </summary>
public class LicenseVerificationResponse
{
    public bool IsValid { get; set; }
}

