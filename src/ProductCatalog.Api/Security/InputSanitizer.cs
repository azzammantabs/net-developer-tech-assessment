using System.Net;
using System.Text.RegularExpressions;

namespace ProductCatalog.Api.Security;

/// <summary>
/// Provides XSS and injection sanitization for all user-supplied string inputs.
/// </summary>
public static partial class InputSanitizer
{
    [GeneratedRegex(@"<script[^>]*>[\s\S]*?</script>|javascript:|on\w+\s*=|<[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DangerousMarkupRegex();

    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Strip script tags, inline event handlers, and raw HTML tags
        var cleaned = DangerousMarkupRegex().Replace(input.Trim(), string.Empty);

        // HTML-encode any remaining special characters to prevent reflected/stored XSS
        return WebUtility.HtmlEncode(cleaned);
    }

    public static bool ContainsSqlOrScriptInjection(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.ToLowerInvariant();
        string[] blockedPatterns =
        [
            "<script",
            "javascript:",
            "onerror=",
            "onload=",
            "--",
            ";--",
            "drop table",
            "union select",
            "xp_cmdshell"
        ];

        return blockedPatterns.Any(pattern => normalized.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
