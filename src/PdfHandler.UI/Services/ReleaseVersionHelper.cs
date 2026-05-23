// PDFハンドラ (PDF Handler)
// Copyright (c) 2025-2026 Goplan. All rights reserved.

using System;

namespace PdfHandler.UI.Services;

internal static class ReleaseVersionHelper
{
    public static Version? ParseTag(string? tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return null;

        var normalized = tagName.Trim().TrimStart('v', 'V');
        return Version.TryParse(normalized, out var version) ? version : null;
    }

    public static int GetMajorVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return 0;

        var part = version.Trim().TrimStart('v', 'V').Split('.')[0];
        return int.TryParse(part, out var major) ? major : 0;
    }

    public static bool IsNewerTag(string latestTag, string suppressedTag)
    {
        var latest = ParseTag(latestTag);
        var suppressed = ParseTag(suppressedTag);
        if (latest != null && suppressed != null)
            return latest > suppressed;

        return string.Compare(
            latestTag.Trim(),
            suppressedTag.Trim(),
            StringComparison.OrdinalIgnoreCase) > 0;
    }
}
