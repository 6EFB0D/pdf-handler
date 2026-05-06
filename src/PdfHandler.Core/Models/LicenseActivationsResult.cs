// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Models;

/// <summary>
/// ライセンスアクティベーション一覧の取得結果
/// </summary>
public class LicenseActivationsResult
{
    /// <summary>
    /// アクティベーション一覧
    /// </summary>
    public IReadOnlyList<DeviceActivation> Activations { get; set; } = Array.Empty<DeviceActivation>();

    /// <summary>
    /// 1ライセンスあたりのデバイス数上限
    /// </summary>
    public int DeviceLimit { get; set; }

    /// <summary>
    /// 現在のアクティベーション数
    /// </summary>
    public int DeviceCount { get; set; }
}
