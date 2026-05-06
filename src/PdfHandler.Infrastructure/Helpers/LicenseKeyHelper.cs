// PDFハンドラ (PDF Handler)
// Copyright (c) 2024-2025 Goplan. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PdfHandler.Infrastructure.Helpers;

/// <summary>
/// ライセンスキーの正規化・表示・HMAC 検証
/// - コンパクト（記号32・Crockford 24）: API/DB 正準形は 4 文字ごとのハイフン区切り（計 32 記号 + 7 ハイフン）
/// - 旧長形式: 16 進シリアル28 + HMAC
/// </summary>
public static class LicenseKeyHelper
{
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private static readonly Regex LongHmacKeyRegex = new(
        @"^([A-Z0-9]{4})-(P[12]|S[12])(\d{2})-([0-9A-Fa-f]{28})-([0-9A-Fa-f]+)$",
        RegexOptions.Compiled);

    private static readonly Regex CompactPlainRegex = new(
        @"^(PDFH|ZIPS|PICT)(P[12]\d{2})([0-9A-HJKMNP-TV-Z]{24})$",
        RegexOptions.Compiled);

    /// <summary>
    /// 正規化: DB 照合・API 送信用。コンパクトは区切り付き、長形式はハイフン最小形。
    /// </summary>
    public static string? Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        key = SanitizePastedLicenseKey(key);

        var compact = NormalizeCompactToStorage(key);
        if (compact != null)
            return compact;

        var trimmed = key.Trim().ToUpperInvariant();
        var parts = trimmed.Split('-');
        if (parts.Length < 4)
            return null;

        if (parts[0].Length != 4 || !IsPrefixAlnum(parts[0]))
            return null;

        var formCode = parts[1];
        if (formCode.Length != 4)
            return null;

        var serialPart = string.Concat(parts.Skip(2).Take(parts.Length - 3));
        var hmacPart = parts[^1];

        if (serialPart.Length != 28 || !IsHex(serialPart))
            return null;
        if (hmacPart.Length == 0 || !IsHex(hmacPart))
            return null;

        return $"{parts[0]}-{formCode}-{serialPart}-{hmacPart}";
    }

    /// <summary>
    /// 旧形式（HMAC なし 28 桁 hex）
    /// </summary>
    public static string? NormalizeLegacy(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        key = SanitizePastedLicenseKey(key);

        var trimmed = key.Trim().ToUpperInvariant();
        var parts = trimmed.Split('-');
        if (parts.Length < 3)
            return null;

        if (parts[0].Length != 4 || !IsPrefixAlnum(parts[0]))
            return null;

        var formCode = parts[1];
        if (formCode.Length != 4)
            return null;

        var serialPart = string.Concat(parts.Skip(2));
        if (serialPart.Length != 28 || !IsHex(serialPart))
            return null;

        return $"{parts[0]}-{formCode}-{serialPart}";
    }

    /// <summary>
    /// ユーザー向け表示（コンパクトは既に区切り済みならそのまま）
    /// </summary>
    public static string FormatForDisplay(string? normalizedKey)
    {
        if (string.IsNullOrEmpty(normalizedKey))
            return "";

        var plain = ToCanonicalPlain32(normalizedKey);
        if (plain != null)
            return FormatStorageFromPlain32(plain);

        var match = LongHmacKeyRegex.Match(normalizedKey);
        if (match.Success)
        {
            var formCode = match.Groups[2].Value + match.Groups[3].Value;
            var serial = match.Groups[4].Value;
            var hmac = match.Groups[5].Value;
            var sb = new StringBuilder();
            for (var i = 0; i < serial.Length; i += 4)
            {
                if (i > 0) sb.Append('-');
                sb.Append(serial.Substring(i, Math.Min(4, serial.Length - i)));
            }
            return $"{match.Groups[1].Value}-{formCode}-{sb}-{hmac}";
        }

        var legacyMatch = Regex.Match(normalizedKey,
            @"^([A-Z0-9]{4})-(P[12]|S[12])(\d{2})-([0-9A-Fa-f]{28})$");
        if (legacyMatch.Success)
        {
            var formCode = legacyMatch.Groups[2].Value + legacyMatch.Groups[3].Value;
            var serial = legacyMatch.Groups[4].Value;
            var sb = new StringBuilder();
            for (var i = 0; i < serial.Length; i += 4)
            {
                if (i > 0) sb.Append('-');
                sb.Append(serial.Substring(i, Math.Min(4, serial.Length - i)));
            }
            return $"{legacyMatch.Groups[1].Value}-{formCode}-{sb}";
        }

        return normalizedKey;
    }

    /// <summary>HMAC オフライン検証（長形式・コンパクト両方）</summary>
    public static bool VerifyHmac(string? normalizedKey, string? secretKey)
    {
        if (string.IsNullOrEmpty(normalizedKey) || string.IsNullOrEmpty(secretKey))
            return false;

        if (VerifyCompactHmac(normalizedKey, secretKey))
            return true;

        var match = LongHmacKeyRegex.Match(normalizedKey);
        if (!match.Success)
            return false;

        var prefix = match.Groups[1].Value;
        var formCode = match.Groups[2].Value + match.Groups[3].Value;
        var serial = match.Groups[4].Value;
        var expectedHmac = match.Groups[5].Value;
        return HmacHexPrefixMatches(prefix, formCode, serial, expectedHmac, secretKey);
    }

    public static bool IsHmacFormat(string? normalizedKey)
    {
        if (string.IsNullOrEmpty(normalizedKey))
            return false;
        if (ToCanonicalPlain32(normalizedKey) != null)
            return true;
        return LongHmacKeyRegex.IsMatch(normalizedKey);
    }

    private static string? NormalizeCompactToStorage(string key)
    {
        var plain = ToCanonicalPlain32(key);
        return plain == null ? null : FormatStorageFromPlain32(plain);
    }

    private static string SanitizePastedLicenseKey(string key)
    {
        var s = key.Trim().Normalize(NormalizationForm.FormKC);
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\u00a0':
                case '\u202f':
                    sb.Append(' ');
                    break;
                case '\ufeff':
                case '\u200b':
                case '\u200c':
                case '\u200d':
                case '\u2060':
                    break;
                case '\u2010':
                case '\u2011':
                case '\u2012':
                case '\u2013':
                case '\u2014':
                case '\u2212':
                case '\uff0d':
                    sb.Append('-');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString().Trim();
    }

    private static string? ToCanonicalPlain32(string key)
    {
        key = SanitizePastedLicenseKey(key);
        var sb = new StringBuilder();
        foreach (var c in key.Trim())
        {
            if (c == '-' || char.IsWhiteSpace(c))
                continue;
            sb.Append(char.ToUpperInvariant(c));
        }

        var s = sb.ToString();
        if (s.Length != 32 || !CompactPlainRegex.IsMatch(s))
            return null;

        var dec = DecodeCrockford24(s.AsSpan(8));
        if (dec == null || dec.Length != 15)
            return null;

        return s[..8] + EncodeCrockford15(dec);
    }

    private static string FormatStorageFromPlain32(string plain32)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < 32; i += 4)
        {
            if (i > 0) sb.Append('-');
            sb.Append(plain32, i, 4);
        }
        return sb.ToString();
    }

    private static bool VerifyCompactHmac(string storageOrPlain, string secretKey)
    {
        var plain = ToCanonicalPlain32(storageOrPlain);
        if (plain == null)
            return false;

        var prefix = plain[..4];
        var formCode = plain.Substring(4, 4);
        var dec = DecodeCrockford24(plain.AsSpan(8));
        if (dec == null || dec.Length != 15)
            return false;

        var serial11 = dec.AsSpan(0, 11);
        var mac4 = dec.AsSpan(11, 4);
        var hex11 = Convert.ToHexString(serial11);
        var message = $"{prefix}:{formCode}:{hex11}";

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        return mac4.SequenceEqual(hash.AsSpan(0, 4));
    }

    private static bool HmacHexPrefixMatches(
        string prefix, string formCode, string serial, string expectedHmac, string secretKey)
    {
        var message = $"{prefix}:{formCode}:{serial}";
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        var computedHmac = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        var compareLen = Math.Min(expectedHmac.Length, Math.Max(8, computedHmac.Length));
        if (compareLen <= 0 || computedHmac.Length < compareLen)
            return false;
        return string.Compare(
            computedHmac[..compareLen],
            expectedHmac[..compareLen],
            StringComparison.OrdinalIgnoreCase) == 0;
    }

    private static string EncodeCrockford15(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 15)
            throw new ArgumentException("need 15 bytes");
        ulong buffer = 0;
        var bits = 0;
        Span<char> chars = stackalloc char[24];
        var n = 0;
        for (var i = 0; i < 15; i++)
        {
            buffer = (buffer << 8) | (ulong)bytes[i];
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                var idx = (int)((buffer >> bits) & 31);
                chars[n++] = CrockfordAlphabet[idx];
            }
        }
        if (bits > 0)
        {
            var idx = (int)((buffer << (5 - bits)) & 31);
            chars[n++] = CrockfordAlphabet[idx];
        }
        return new string(chars[..24]);
    }

    private static byte[]? DecodeCrockford24(ReadOnlySpan<char> s)
    {
        if (s.Length != 24)
            return null;
        Span<int> rev = stackalloc int[256];
        rev.Fill(-1);
        for (var i = 0; i < CrockfordAlphabet.Length; i++)
        {
            rev[CrockfordAlphabet[i]] = i;
        }
        ulong buffer = 0;
        var bits = 0;
        var outList = new List<byte>(15);
        for (var j = 0; j < s.Length; j++)
        {
            var c = s[j];
            if (c >= 256)
                return null;
            var idx = rev[c];
            if (idx < 0)
                return null;
            buffer = (buffer << 5) | (ulong)(uint)idx;
            bits += 5;
            while (bits >= 8)
            {
                bits -= 8;
                outList.Add((byte)((buffer >> bits) & 0xFFUL));
            }
        }
        return outList.Count == 15 ? outList.ToArray() : null;
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

    private static bool IsPrefixAlnum(string s)
    {
        foreach (var c in s)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')))
                return false;
        }
        return true;
    }
}
