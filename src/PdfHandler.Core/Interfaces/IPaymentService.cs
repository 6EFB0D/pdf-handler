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
    /// 購入手続きメールの送信を要求する（買い切り Standard のみ。サブスク新規販売は終了）
    /// </summary>
    /// <param name="plan">ライセンスプラン（Standard版・買い切りのみ）</param>
    /// <param name="customerEmail">ライセンス送付先メールアドレス。この宛先に Stripe Checkout リンク入りのメールが送信される。</param>
    /// <returns>送信結果。Success=true の場合、指定メールアドレスに決済リンクメールが送信されている。</returns>
    /// <remarks>
    /// 以前は Checkout URL を返してアプリから直接ブラウザで開く方式だったが、
    /// メールアドレスの誤入力を決済前に検知できるよう「メール先行 → リンクから Stripe」方式へ変更。
    /// </remarks>
    Task<RequestCheckoutResult> RequestCheckoutAsync(LicensePlan plan, string customerEmail);

    /// <summary>
    /// ライセンスキーを検証
    /// </summary>
    Task<bool> VerifyLicenseKeyAsync(string licenseKey);
}






