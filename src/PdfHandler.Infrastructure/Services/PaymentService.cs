// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net.Http;
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
    /// Stripe Checkoutセッションを作成
    /// </summary>
    public async Task<string> CreateCheckoutSessionAsync(LicensePlan plan, bool isSubscription)
    {
        try
        {
            // Premium版が非公開の場合は、Premiumプランの選択を拒否
            if (!_settings.EnablePremiumPlan && plan == LicensePlan.Premium)
            {
                throw new InvalidOperationException("Premiumプランは現在公開されていません。");
            }

            var request = new
            {
                plan = plan.ToString(),
                isSubscription = isSubscription
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUrl = $"{_settings.Supabase.Url}/functions/v1/create-checkout-session";
            DebugLogger.WriteLine($"=== Checkoutセッション作成開始 ===");
            DebugLogger.WriteLine($"リクエストURL: {requestUrl}");
            DebugLogger.WriteLine($"リクエストボディ: {json}");
            
            var response = await _httpClient.PostAsync(requestUrl, content);
            
            DebugLogger.WriteLine($"レスポンスステータス: {response.StatusCode}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            DebugLogger.WriteLine($"レスポンス内容: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = JsonSerializer.Deserialize<CheckoutSessionResponse>(responseContent, options);
                
                DebugLogger.WriteLine($"デシリアライズ結果: result={result != null}, CheckoutUrl={result?.CheckoutUrl ?? "null"}");
                
                if (result == null || string.IsNullOrWhiteSpace(result.CheckoutUrl))
                {
                    throw new Exception($"Checkout URLが取得できませんでした。レスポンス: {responseContent}");
                }
                return result.CheckoutUrl;
            }
            else
            {
                throw new Exception($"Checkoutセッション作成エラー: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.WriteLine($"Checkoutセッション作成エラー: {ex.GetType().Name} - {ex.Message}");
            DebugLogger.WriteLine($"スタックトレース: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                DebugLogger.WriteLine($"内部例外: {ex.InnerException.Message}");
            }
            throw;
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
/// Checkoutセッション作成レスポンス
/// </summary>
public class CheckoutSessionResponse
{
    [JsonPropertyName("checkoutUrl")]
    public string CheckoutUrl { get; set; } = string.Empty;
}

/// <summary>
/// ライセンスキー検証レスポンス
/// </summary>
public class LicenseVerificationResponse
{
    public bool IsValid { get; set; }
}

