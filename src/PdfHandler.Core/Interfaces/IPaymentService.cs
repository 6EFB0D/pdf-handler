// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using PdfHandler.Core.Models;

namespace PdfHandler.Core.Interfaces;

/// <summary>
/// 決済サービスのインターフェース
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Stripe Checkoutセッションを作成
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(LicensePlan plan, bool isSubscription);

    /// <summary>
    /// ライセンスキーを検証
    /// </summary>
    Task<bool> VerifyLicenseKeyAsync(string licenseKey);
}






