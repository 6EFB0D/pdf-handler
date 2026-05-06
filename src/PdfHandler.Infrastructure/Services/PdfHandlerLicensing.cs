// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace PdfHandler.Infrastructure.Services;

/// <summary>
/// Supabase Edge（verify-license 等）に送る製品コード。<c>stripe</c> / checkout の metadata <c>app_id</c> と一致させること。
/// </summary>
public static class PdfHandlerLicensing
{
    public const string ClientAppId = "PDFH";
}
