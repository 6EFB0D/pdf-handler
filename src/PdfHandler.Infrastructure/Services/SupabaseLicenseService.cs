// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Text;
using System.Text.Json;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.Infrastructure.Configuration;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// Supabase経由のライセンス管理サービス
/// </summary>
public class SupabaseLicenseService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private readonly string _hardwareId;

    public SupabaseLicenseService(AppSettings settings, string hardwareId)
    {
        _settings = settings;
        _hardwareId = hardwareId;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("apikey", _settings.Supabase.AnonKey);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.Supabase.AnonKey}");
    }

    /// <summary>
    /// ライセンスキーとハードウェアIDを検証
    /// </summary>
    public async Task<LicenseVerificationResult> VerifyLicenseAsync(string licenseKey)
    {
        try
        {
            var request = new
            {
                licenseKey = licenseKey,
                hardwareId = _hardwareId
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_settings.Supabase.Url}/functions/v1/verify-license",
                content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LicenseVerificationResult>(responseJson);
                return result ?? new LicenseVerificationResult { IsValid = false };
            }

            return new LicenseVerificationResult { IsValid = false };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ライセンス検証エラー: {ex.Message}");
            return new LicenseVerificationResult { IsValid = false };
        }
    }

    /// <summary>
    /// ライセンスをアクティベーション
    /// </summary>
    public async Task<LicenseActivationResult> ActivateLicenseAsync(string licenseKey, string userEmail)
    {
        try
        {
            var request = new
            {
                licenseKey = licenseKey,
                hardwareId = _hardwareId,
                userEmail = userEmail
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_settings.Supabase.Url}/functions/v1/activate-license",
                content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LicenseActivationResult>(responseJson);
                return result ?? new LicenseActivationResult { Success = false };
            }

            return new LicenseActivationResult { Success = false };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ライセンスアクティベーションエラー: {ex.Message}");
            return new LicenseActivationResult { Success = false };
        }
    }
}

/// <summary>
/// ライセンス検証結果
/// </summary>
public class LicenseVerificationResult
{
    public bool IsValid { get; set; }
    public LicensePlan? Plan { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? LastVerificationDate { get; set; }
    public DateTime? NextVerificationDate { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// ライセンスアクティベーション結果
/// </summary>
public class LicenseActivationResult
{
    public bool Success { get; set; }
    public LicensePlan? Plan { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? ErrorMessage { get; set; }
}



