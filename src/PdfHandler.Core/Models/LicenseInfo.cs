// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Models;

/// <summary>
/// ライセンス情報を表すモデル
/// </summary>
public class LicenseInfo
{
    /// <summary>
    /// ライセンスプラン
    /// </summary>
    public LicensePlan Plan { get; set; } = LicensePlan.Trial;

    /// <summary>
    /// ライセンスキー（暗号化済み）
    /// </summary>
    public string? LicenseKey { get; set; }

    /// <summary>
    /// 初回起動日時（試用期間の開始日時）
    /// </summary>
    public DateTime FirstLaunchDate { get; set; } = DateTime.Now;

    /// <summary>
    /// ライセンス有効期限（買い切り版の場合）
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// サブスクリプションの更新日時
    /// </summary>
    public DateTime? SubscriptionRenewalDate { get; set; }

    /// <summary>
    /// ハードウェアID（マシン固有ID）
    /// </summary>
    public string? HardwareId { get; set; }

    /// <summary>
    /// アクティベーション日時
    /// </summary>
    public DateTime? ActivationDate { get; set; }

    /// <summary>
    /// 最終検証日時（オンライン検証の最終実行日時）
    /// </summary>
    public DateTime? LastVerificationDate { get; set; }

    /// <summary>
    /// 次回検証日時（30日後の検証が必要な日時）
    /// </summary>
    public DateTime? NextVerificationDate { get; set; }

    /// <summary>
    /// 購入時のメジャーバージョン（買い切り版のみ。サブスクはnullで全バージョン対応）
    /// </summary>
    public string? PurchasedVersion { get; set; }

    /// <summary>
    /// 試用期間の残り日数を取得
    /// </summary>
    public int GetRemainingTrialDays()
    {
        if (Plan != LicensePlan.Trial)
            return 0;

        var elapsed = DateTime.Now - FirstLaunchDate;
        var remaining = 14 - elapsed.TotalDays; // 14日間に変更
        return Math.Max(0, (int)Math.Ceiling(remaining));
    }

    /// <summary>
    /// 試用期間が有効かどうかを判定
    /// </summary>
    public bool IsTrialValid()
    {
        if (Plan != LicensePlan.Trial)
            return false;

        var elapsed = DateTime.Now - FirstLaunchDate;
        return elapsed.TotalDays < 14; // 14日間に変更
    }

    /// <summary>
    /// 検証が必要かどうかを判定（30日ごとの検証）
    /// </summary>
    public bool IsVerificationRequired()
    {
        // 試用期間中は検証不要
        if (Plan == LicensePlan.Trial)
            return false;

        // 買い切り版は検証不要（永続的に有効）
        if (Plan == LicensePlan.StandardPurchased)
            return false;

        // サブスクリプション版は30日ごとに検証が必要
        if (NextVerificationDate.HasValue)
        {
            return DateTime.Now >= NextVerificationDate.Value;
        }

        // NextVerificationDateが設定されていない場合は、ActivationDateから30日後を計算
        if (ActivationDate.HasValue)
        {
            var nextVerification = ActivationDate.Value.AddDays(30);
            return DateTime.Now >= nextVerification;
        }

        // ActivationDateもない場合は検証が必要
        return true;
    }

    /// <summary>
    /// ライセンスが有効かどうかを判定
    /// </summary>
    public bool IsLicenseValid()
    {
        // 試用期間中
        if (Plan == LicensePlan.Trial && IsTrialValid())
            return true;

        // 買い切り版
        if (Plan == LicensePlan.StandardPurchased)
            return true; // 買い切り版は永続的に有効

        // サブスクリプション版
        if (Plan == LicensePlan.StandardSubscription || Plan == LicensePlan.Premium || Plan == LicensePlan.PremiumBYOK)
        {
            if (SubscriptionRenewalDate.HasValue)
                return DateTime.Now <= SubscriptionRenewalDate.Value;
            return false;
        }

        return false;
    }
}

/// <summary>
/// ライセンスプラン
/// </summary>
public enum LicensePlan
{
    /// <summary>
    /// 試用期間中
    /// </summary>
    Trial = 0,

    /// <summary>
    /// Standard買い切り版
    /// </summary>
    StandardPurchased = 1,

    /// <summary>
    /// Standardサブスクリプション版
    /// </summary>
    StandardSubscription = 2,

    /// <summary>
    /// Premium版
    /// </summary>
    Premium = 3,

    /// <summary>
    /// Premium BYOK版
    /// </summary>
    PremiumBYOK = 4
}




