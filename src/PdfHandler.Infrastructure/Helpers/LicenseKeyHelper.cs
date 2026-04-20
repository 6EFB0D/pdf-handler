// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfHandler.Infrastructure.Helpers;

/// <summary>
/// ライセンスキーの正規化・表示フォーマット・HMAC検証
/// license-code-specification.md 準拠
/// </summary>
public static class LicenseKeyHelper
{
    private static readonly Regex HmacKeyRegex = new(
        @"^PDFH-(P[12]|S[12])(\d{2})-([0-9A-Fa-f]{28})-([0-9A-Fa-f]+)$",
        RegexOptions.Compiled);

    /// <summary>
    /// 正規化: 4桁区切り形式の入力を標準形式に変換（ハイフン除去してシリアル・HMACを連結）
    /// 例: PDFH-P101-A1B2-C3D4-...-M3-1A2B → PDFH-P101-A1B2C3D4E5F6G7H8I9J0K1L2M3-1A2B
    /// </summary>
    public static string? Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var trimmed = key.Trim().ToUpperInvariant();
        var parts = trimmed.Split('-');
        if (parts.Length < 4)
            return null;

        if (parts[0] != "PDFH")
            return null;

        var formCode = parts[1];
        if (formCode.Length != 4)
            return null;

        // parts[2]..parts[^1] がシリアル（ハイフン区切りかもしれない）
        // parts[last] が HMAC
        var serialPart = string.Concat(parts.Skip(2).Take(parts.Length - 3));
        var hmacPart = parts[^1];

        if (serialPart.Length != 28 || !IsHex(serialPart))
            return null;
        if (hmacPart.Length == 0 || !IsHex(hmacPart))
            return null;

        return $"PDFH-{formCode}-{serialPart}-{hmacPart}";
    }

    /// <summary>
    /// 旧形式（HMACなし PDFH-P101-28文字）の正規化
    /// 4桁区切り入力にも対応（PDFH-P101-A1B2-C3D4-...-M3）
    /// </summary>
    public static string? NormalizeLegacy(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var trimmed = key.Trim().ToUpperInvariant();
        var parts = trimmed.Split('-');
        if (parts.Length < 3 || parts[0] != "PDFH")
            return null;

        var formCode = parts[1];
        if (formCode.Length != 4)
            return null;

        var serialPart = string.Concat(parts.Skip(2));
        if (serialPart.Length != 28 || !IsHex(serialPart))
            return null;

        return $"PDFH-{formCode}-{serialPart}";
    }

    /// <summary>
    /// 表示用に4桁区切りでフォーマット
    /// 例: PDFH-P101-A1B2C3D4... → PDFH-P101-A1B2-C3D4-E5F6-G7H8-I9J0-K1L2-M3
    /// HMAC部があれば末尾に付加（4桁区切りしない）
    /// </summary>
    public static string FormatForDisplay(string? normalizedKey)
    {
        if (string.IsNullOrEmpty(normalizedKey))
            return "";

        var match = HmacKeyRegex.Match(normalizedKey);
        if (match.Success)
        {
            var formCode = match.Groups[1].Value + match.Groups[2].Value;
            var serial = match.Groups[3].Value;
            var hmac = match.Groups[4].Value;

            // シリアルを4桁区切り
            var sb = new StringBuilder();
            for (var i = 0; i < serial.Length; i += 4)
            {
                if (i > 0) sb.Append('-');
                sb.Append(serial.Substring(i, Math.Min(4, serial.Length - i)));
            }
            return $"PDFH-{formCode}-{sb}-{hmac}";
        }

        // 旧形式
        var legacyMatch = Regex.Match(normalizedKey, @"^PDFH-(P[12]|S[12])(\d{2})-([0-9A-Fa-f]{28})$");
        if (legacyMatch.Success)
        {
            var formCode = legacyMatch.Groups[1].Value + legacyMatch.Groups[2].Value;
            var serial = legacyMatch.Groups[3].Value;
            var sb = new StringBuilder();
            for (var i = 0; i < serial.Length; i += 4)
            {
                if (i > 0) sb.Append('-');
                sb.Append(serial.Substring(i, Math.Min(4, serial.Length - i)));
            }
            return $"PDFH-{formCode}-{sb}";
        }

        return normalizedKey;
    }

    /// <summary>
    /// HMACオフライン検証
    /// 署名対象: PDFH:P101:{シリアル部}
    /// </summary>
    public static bool VerifyHmac(string? normalizedKey, string? secretKey)
    {
        if (string.IsNullOrEmpty(normalizedKey) || string.IsNullOrEmpty(secretKey))
            return false;

        var match = HmacKeyRegex.Match(normalizedKey);
        if (!match.Success)
            return false;

        var formCode = match.Groups[1].Value + match.Groups[2].Value;
        var serial = match.Groups[3].Value;
        var expectedHmac = match.Groups[4].Value;

        var message = $"PDFH:{formCode}:{serial}";
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        var computedHmac = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();

        // 署名は先頭8文字以上で比較（stripe-webhook と同期）
        var compareLen = Math.Min(expectedHmac.Length, Math.Max(8, computedHmac.Length));
        if (compareLen <= 0 || computedHmac.Length < compareLen)
            return false;
        return string.Compare(computedHmac.Substring(0, compareLen),
            expectedHmac.Substring(0, compareLen),
            StringComparison.OrdinalIgnoreCase) == 0;
    }

    /// <summary>
    /// HMAC付き形式かどうか
    /// </summary>
    public static bool IsHmacFormat(string? normalizedKey)
    {
        return !string.IsNullOrEmpty(normalizedKey) && HmacKeyRegex.IsMatch(normalizedKey);
    }

    private static bool IsHex(string s)
    {
        foreach (var c in s)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }
}
