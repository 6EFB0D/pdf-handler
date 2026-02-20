// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PdfHandler.Core.Interfaces;
using PdfHandler.Core.Models;
using PdfHandler.Infrastructure.Configuration;

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// ライセンス管理サービスの実装
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly string _licenseFilePath;
    private LicenseInfo? _currentLicense;
    private readonly string _hardwareId;
    private readonly AppSettings _settings;
    private readonly HttpClient _httpClient;

    public LicenseService(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pdfHandlerPath = Path.Combine(appDataPath, "PDFHandler");

        if (!Directory.Exists(pdfHandlerPath))
        {
            Directory.CreateDirectory(pdfHandlerPath);
        }

        _licenseFilePath = Path.Combine(pdfHandlerPath, "license.json");
        _hardwareId = GenerateHardwareId();

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("apikey", _settings.Supabase.AnonKey);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.Supabase.AnonKey}");
    }

    /// <summary>
    /// ハードウェアIDを生成
    /// </summary>
    private string GenerateHardwareId()
    {
        try
        {
            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var osVersion = Environment.OSVersion.ToString();
            var processorCount = Environment.ProcessorCount.ToString();

            var combined = $"{machineName}|{userName}|{osVersion}|{processorCount}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hash);
        }
        catch
        {
            // フォールバック: ランダムなIDを生成
            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// 現在のライセンス情報を取得
    /// </summary>
    public LicenseInfo GetLicenseInfo()
    {
        if (_currentLicense == null)
        {
            LoadLicenseAsync().Wait();
        }
        return _currentLicense ?? new LicenseInfo { HardwareId = _hardwareId };
    }

    /// <summary>
    /// 復号したライセンスキーを取得（表示・コピー用）
    /// </summary>
    public string? GetLicenseKey()
    {
        return DecryptLicenseKey(GetLicenseInfo().LicenseKey);
    }

    /// <summary>
    /// ライセンス情報を読み込み
    /// </summary>
    public async Task LoadLicenseAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                if (!File.Exists(_licenseFilePath))
                {
                    // 初回起動: 試用期間を開始
                    _currentLicense = new LicenseInfo
                    {
                        Plan = LicensePlan.Trial,
                        FirstLaunchDate = DateTime.Now,
                        HardwareId = _hardwareId
                    };
                    await SaveLicenseAsync(_currentLicense);
                    return;
                }

                var json = File.ReadAllText(_licenseFilePath);
                var license = JsonSerializer.Deserialize<LicenseInfo>(json);

                if (license != null)
                {
                    // ハードウェアIDが一致しない場合は試用期間をリセット
                    if (license.HardwareId != _hardwareId)
                    {
                        license = new LicenseInfo
                        {
                            Plan = LicensePlan.Trial,
                            FirstLaunchDate = DateTime.Now,
                            HardwareId = _hardwareId
                        };
                        await SaveLicenseAsync(license);
                    }
                    _currentLicense = license;
                }
                else
                {
                    _currentLicense = new LicenseInfo
                    {
                        Plan = LicensePlan.Trial,
                        FirstLaunchDate = DateTime.Now,
                        HardwareId = _hardwareId
                    };
                    await SaveLicenseAsync(_currentLicense);
                }
            }
            catch (Exception ex)
            {
                // エラー時は試用期間を開始
                System.Diagnostics.Debug.WriteLine($"ライセンス読み込みエラー: {ex.Message}");
                _currentLicense = new LicenseInfo
                {
                    Plan = LicensePlan.Trial,
                    FirstLaunchDate = DateTime.Now,
                    HardwareId = _hardwareId
                };
                try
                {
                    await SaveLicenseAsync(_currentLicense);
                }
                catch
                {
                    // 保存エラーは無視
                }
            }
        });
    }

    /// <summary>
    /// ライセンス情報を保存
    /// </summary>
    public async Task SaveLicenseAsync(LicenseInfo licenseInfo)
    {
        await Task.Run(() =>
        {
            try
            {
                licenseInfo.HardwareId = _hardwareId;
                var json = JsonSerializer.Serialize(licenseInfo, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_licenseFilePath, json);
                _currentLicense = licenseInfo;
            }
            catch
            {
                // 保存エラーは無視
            }
        });
    }

    /// <summary>
    /// ライセンスキーでアクティベーション（Supabase verify-license で検証）
    /// </summary>
    public async Task<bool> ActivateLicenseAsync(string licenseKey)
    {
        var trimmedKey = licenseKey?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(trimmedKey))
        {
            return false;
        }

        try
        {
            var deviceName = Environment.MachineName;
            var request = new { licenseKey = trimmedKey, hardwareId = _hardwareId, deviceName };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_settings.Supabase.Url}/functions/v1/verify-license",
                content);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"verify-license HTTPエラー: {response.StatusCode}");
                return false;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<VerifyLicenseResponse>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || !result.IsValid)
            {
                System.Diagnostics.Debug.WriteLine($"verify-license 検証失敗: {result?.ErrorMessage}");
                return false;
            }

            // サーバーからのプラン情報でローカルライセンスを更新
            var plan = MapPlanFromServer(result.Plan);
            var license = GetLicenseInfo();

            license.Plan = plan;
            license.LicenseKey = EncryptLicenseKey(trimmedKey);
            license.ActivationDate = DateTime.Now;
            license.LastVerificationDate = result.LastVerificationDate;
            license.NextVerificationDate = result.NextVerificationDate;
            license.ExpirationDate = result.ExpirationDate;
            license.SubscriptionRenewalDate = result.SubscriptionRenewalDate;

            await SaveLicenseAsync(license);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"アクティベーションエラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// サーバー応答のプラン文字列をLicensePlanに変換
    /// </summary>
    private static LicensePlan MapPlanFromServer(string? plan)
    {
        return plan switch
        {
            "purchased" => LicensePlan.StandardPurchased,
            "subscription_standard" => LicensePlan.StandardSubscription,
            "subscription_premium" => LicensePlan.Premium,
            _ => LicensePlan.StandardPurchased
        };
    }

    /// <summary>
    /// verify-license API レスポンス
    /// </summary>
    private sealed class VerifyLicenseResponse
    {
        public bool IsValid { get; set; }
        public string? Plan { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? SubscriptionRenewalDate { get; set; }
        public DateTime? LastVerificationDate { get; set; }
        public DateTime? NextVerificationDate { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// ライセンスキーを暗号化（簡易版）
    /// </summary>
    private string EncryptLicenseKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// ライセンスキーを復号
    /// </summary>
    private string? DecryptLicenseKey(string? encryptedKey)
    {
        if (string.IsNullOrEmpty(encryptedKey))
            return null;
        try
        {
            var bytes = Convert.FromBase64String(encryptedKey);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// アクティベーション一覧を取得
    /// </summary>
    public async Task<LicenseActivationsResult?> GetActivationsAsync()
    {
        var licenseKey = DecryptLicenseKey(GetLicenseInfo().LicenseKey);
        if (string.IsNullOrEmpty(licenseKey))
            return null;

        try
        {
            var request = new { licenseKey, hardwareId = _hardwareId };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_settings.Supabase.Url}/functions/v1/get-activations",
                content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GetActivationsResponse>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Activations == null)
                return null;

            var activations = result.Activations.Select(a => new DeviceActivation
            {
                Id = a.Id ?? "",
                IsCurrentDevice = a.IsCurrentDevice,
                DisplayName = a.DisplayName ?? "",
                DeviceName = a.DeviceName,
                ActivationDate = a.ActivationDate,
                LastVerificationDate = a.LastVerificationDate,
            }).ToList();

            return new LicenseActivationsResult
            {
                Activations = activations,
                DeviceLimit = result.DeviceLimit,
                DeviceCount = result.DeviceCount,
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetActivationsAsync エラー: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 指定デバイスのアクティベーションを解除
    /// </summary>
    public async Task<bool> DeactivateDeviceAsync(string activationId)
    {
        var licenseKey = DecryptLicenseKey(GetLicenseInfo().LicenseKey);
        if (string.IsNullOrEmpty(licenseKey) || string.IsNullOrEmpty(activationId))
            return false;

        try
        {
            var request = new { licenseKey, hardwareId = _hardwareId, activationId };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_settings.Supabase.Url}/functions/v1/deactivate-device",
                content);

            if (!response.IsSuccessStatusCode)
                return false;

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DeactivateDeviceResponse>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeactivateDeviceAsync エラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 指定デバイスの表示名を更新
    /// </summary>
    public async Task<bool> UpdateDeviceDisplayNameAsync(string activationId, string displayName)
    {
        var licenseKey = DecryptLicenseKey(GetLicenseInfo().LicenseKey);
        if (string.IsNullOrEmpty(licenseKey) || string.IsNullOrEmpty(activationId))
            return false;

        try
        {
            var request = new { licenseKey, hardwareId = _hardwareId, activationId, displayName = displayName ?? "" };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_settings.Supabase.Url}/functions/v1/update-device-display-name",
                content);

            if (!response.IsSuccessStatusCode)
                return false;

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<UpdateDisplayNameResponse>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateDeviceDisplayNameAsync エラー: {ex.Message}");
            return false;
        }
    }

    private sealed class GetActivationsResponse
    {
        public List<ActivationItem>? Activations { get; set; }
        public int DeviceLimit { get; set; }
        public int DeviceCount { get; set; }
    }

    private sealed class ActivationItem
    {
        public string? Id { get; set; }
        public bool IsCurrentDevice { get; set; }
        public string? DisplayName { get; set; }
        public string? DeviceName { get; set; }
        public DateTime? ActivationDate { get; set; }
        public DateTime? LastVerificationDate { get; set; }
    }

    private sealed class DeactivateDeviceResponse
    {
        public bool Success { get; set; }
    }

    private sealed class UpdateDisplayNameResponse
    {
        public bool Success { get; set; }
    }

    /// <summary>
    /// ライセンスが有効かどうかを判定
    /// </summary>
    public bool IsLicenseValid()
    {
        var license = GetLicenseInfo();
        return license.IsLicenseValid();
    }

    /// <summary>
    /// 試用期間が有効かどうかを判定
    /// </summary>
    public bool IsTrialValid()
    {
        var license = GetLicenseInfo();
        return license.IsTrialValid();
    }

    /// <summary>
    /// 試用期間の残り日数を取得
    /// </summary>
    public int GetRemainingTrialDays()
    {
        var license = GetLicenseInfo();
        return license.GetRemainingTrialDays();
    }

    /// <summary>
    /// 機能が利用可能かどうかを判定
    /// </summary>
    public bool CanUseFeature(string featureName)
    {
        if (!IsLicenseValid() && !IsTrialValid())
            return false;

        // 試用期間中は全機能利用可能
        if (IsTrialValid())
            return true;

        var license = GetLicenseInfo();
        var plan = license.Plan;

        return featureName switch
        {
            "Merge" => plan != LicensePlan.Trial,
            "Split" => plan != LicensePlan.Trial,
            "Rotate" => true, // 無料版でも利用可能
            "WorkFolder" => plan != LicensePlan.Trial,
            "Rename" => plan != LicensePlan.Trial, // ファイル名変更は有償版のみ
            // "Binder" => plan >= LicensePlan.StandardPurchased, // 一旦無効化
            // "PrintDriver" => plan >= LicensePlan.StandardPurchased, // 一旦無効化
            "AI" => plan >= LicensePlan.Premium,
            _ => false
        };
    }

    /// <summary>
    /// PDF結合機能が利用可能かどうか
    /// </summary>
    public bool CanUseMerge()
    {
        return CanUseFeature("Merge");
    }

    /// <summary>
    /// PDF分割機能が利用可能かどうか
    /// </summary>
    public bool CanUseSplit()
    {
        return CanUseFeature("Split");
    }

    /// <summary>
    /// PDF回転機能が利用可能かどうか
    /// </summary>
    public bool CanUseRotate()
    {
        return CanUseFeature("Rotate");
    }

    /// <summary>
    /// 専用ワークフォルダ機能が利用可能かどうか
    /// </summary>
    public bool CanUseWorkFolder()
    {
        return CanUseFeature("WorkFolder");
    }

    /// <summary>
    /// PDFバインダー機能が利用可能かどうか
    /// </summary>
    public bool CanUseBinder()
    {
        return CanUseFeature("Binder");
    }

    /// <summary>
    /// プリンタドライバ機能が利用可能かどうか
    /// </summary>
    public bool CanUsePrintDriver()
    {
        return CanUseFeature("PrintDriver");
    }

    /// <summary>
    /// AI機能が利用可能かどうか
    /// </summary>
    public bool CanUseAI()
    {
        return CanUseFeature("AI");
    }

    /// <summary>
    /// ファイル名変更機能が利用可能かどうか
    /// </summary>
    public bool CanUseRename()
    {
        return CanUseFeature("Rename");
    }

    /// <summary>
    /// ライセンスを検証（定期的なオンライン検証用）
    /// </summary>
    public async Task<bool> VerifyLicenseAsync()
    {
        // TODO: Supabase経由でオンライン検証を実装
        // 現時点では簡易実装（常にtrueを返す）
        await Task.CompletedTask;
        return true;
    }
}

