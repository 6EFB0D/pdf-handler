// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Core.Models;

/// <summary>
/// 購入手続きメール送信リクエストの結果
/// </summary>
/// <remarks>
/// request-checkout Edge Function のレスポンスに対応。
/// Success=true の場合、指定メールアドレスに Stripe Checkout リンク入りのメールが送信されている。
/// </remarks>
public class RequestCheckoutResult
{
    /// <summary>
    /// メール送信が成功したかどうか
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// マスク化された送信先メールアドレス（例: "k***@gmail.com"）
    /// UI でユーザーに「どこに送ったか」を伝える用途
    /// </summary>
    public string EmailMasked { get; set; } = string.Empty;

    /// <summary>
    /// サーバからのメッセージ（例: "お支払い用のメールを送信しました"）
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
