// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Models;

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// ライセンス管理サービスのインターフェース
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// アクティベーション一覧を取得（ライセンス管理ダイアログ用）
    /// </summary>
    Task<LicenseActivationsResult?> GetActivationsAsync();

    /// <summary>
    /// 指定デバイスのアクティベーションを解除
    /// </summary>
    Task<bool> DeactivateDeviceAsync(string activationId);

    /// <summary>
    /// 指定デバイスの表示名を更新
    /// </summary>
    Task<bool> UpdateDeviceDisplayNameAsync(string activationId, string displayName);

    /// <summary>
    /// 現在のライセンス情報を取得
    /// </summary>
    LicenseInfo GetLicenseInfo();

    /// <summary>
    /// 復号したライセンスキーを取得（表示・コピー用。無い場合はnull）
    /// </summary>
    string? GetLicenseKey();

    /// <summary>
    /// ライセンス情報を読み込み
    /// </summary>
    Task LoadLicenseAsync();

    /// <summary>
    /// ライセンス情報を保存
    /// </summary>
    Task SaveLicenseAsync(LicenseInfo licenseInfo);

    /// <summary>
    /// ライセンスキーでアクティベーション
    /// </summary>
    Task<bool> ActivateLicenseAsync(string licenseKey);

    /// <summary>
    /// ライセンスを検証（定期的なオンライン検証用）
    /// </summary>
    Task<bool> VerifyLicenseAsync();

    /// <summary>
    /// ライセンスが有効かどうかを判定
    /// </summary>
    bool IsLicenseValid();

    /// <summary>
    /// 試用期間が有効かどうかを判定
    /// </summary>
    bool IsTrialValid();

    /// <summary>
    /// 試用期間の残り日数を取得
    /// </summary>
    int GetRemainingTrialDays();

    /// <summary>
    /// 機能が利用可能かどうかを判定
    /// </summary>
    bool CanUseFeature(string featureName);

    /// <summary>
    /// PDF結合機能が利用可能かどうか
    /// </summary>
    bool CanUseMerge();

    /// <summary>
    /// PDF分割機能が利用可能かどうか
    /// </summary>
    bool CanUseSplit();

    /// <summary>
    /// PDF回転機能が利用可能かどうか
    /// </summary>
    bool CanUseRotate();

    /// <summary>
    /// 専用ワークフォルダ機能が利用可能かどうか
    /// </summary>
    bool CanUseWorkFolder();

    /// <summary>
    /// PDFバインダー機能が利用可能かどうか
    /// </summary>
    bool CanUseBinder();

    /// <summary>
    /// プリンタドライバ機能が利用可能かどうか
    /// </summary>
    bool CanUsePrintDriver();

    /// <summary>
    /// AI機能が利用可能かどうか
    /// </summary>
    bool CanUseAI();

    /// <summary>
    /// ファイル名変更機能が利用可能かどうか
    /// </summary>
    bool CanUseRename();
}




