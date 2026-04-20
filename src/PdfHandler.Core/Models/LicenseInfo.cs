// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Models;

/// <summary>
/// ライセンス情報を表すモデル（試用 / 買い切り Standard のみ）
/// </summary>
public class LicenseInfo
{
    public LicensePlan Plan { get; set; } = LicensePlan.Trial;

    public string? LicenseKey { get; set; }

    public DateTime FirstLaunchDate { get; set; } = DateTime.Now;

    public DateTime? ExpirationDate { get; set; }

    public string? HardwareId { get; set; }

    public DateTime? ActivationDate { get; set; }

    public DateTime? LastVerificationDate { get; set; }

    public DateTime? NextVerificationDate { get; set; }

    /// <summary>買い切り時のメジャーバージョン（オンライン検証で設定）</summary>
    public string? PurchasedVersion { get; set; }

    public DateTime? LastSuccessfulOnlineVerificationAt { get; set; }

    public int GetRemainingTrialDays()
    {
        if (Plan != LicensePlan.Trial)
            return 0;

        var elapsed = DateTime.Now - FirstLaunchDate;
        var remaining = 14 - elapsed.TotalDays;
        return Math.Max(0, (int)Math.Ceiling(remaining));
    }

    public bool IsTrialValid()
    {
        if (Plan != LicensePlan.Trial)
            return false;

        var elapsed = DateTime.Now - FirstLaunchDate;
        return elapsed.TotalDays < 14;
    }

    public bool IsVerificationRequired()
    {
        if (Plan == LicensePlan.Trial)
            return false;
        if (Plan == LicensePlan.StandardPurchased)
            return false;
        return false;
    }

    public bool IsLicenseValid()
    {
        if (Plan == LicensePlan.Trial && IsTrialValid())
            return true;

        if (Plan == LicensePlan.StandardPurchased)
            return true;

        return false;
    }
}

/// <summary>
/// ライセンスプラン（買い切りのみの製品では Trial と StandardPurchased のみ使用）
/// </summary>
public enum LicensePlan
{
    Trial = 0,
    StandardPurchased = 1,
}
