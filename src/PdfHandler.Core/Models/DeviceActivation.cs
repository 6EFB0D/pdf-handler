// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Models;

/// <summary>
/// ライセンスに紐づくデバイスアクティベーション情報（ライセンス管理用）
/// </summary>
public class DeviceActivation
{
    /// <summary>
    /// アクティベーションID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 現在のPCかどうか
    /// </summary>
    public bool IsCurrentDevice { get; set; }

    /// <summary>
    /// 表示名（display_name ?? device_name ?? "デバイスN"）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// PC名（Environment.MachineName、編集不可）
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// アクティベート日時
    /// </summary>
    public DateTime? ActivationDate { get; set; }

    /// <summary>
    /// 最終検証日時
    /// </summary>
    public DateTime? LastVerificationDate { get; set; }
}
