using System.Text.RegularExpressions;

namespace MamiMotovlog.Services;
public static partial class YouTubeParser
{
    public static string? GetVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttps) return null;
        var host = uri.Host.ToLowerInvariant();
        string? candidate = null;
        if (host is "youtu.be" or "www.youtu.be") candidate = uri.AbsolutePath.Trim('/').Split('/')[0];
        else if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com")
        {
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            if (parts.Length > 1 && parts[0] is "shorts" or "embed" or "live") candidate = parts[1];
            else if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var field in uri.Query.TrimStart('?').Split('&'))
                {
                    var pair = field.Split('=', 2);
                    if (pair.Length == 2 && pair[0] == "v") { candidate = Uri.UnescapeDataString(pair[1]); break; }
                }
            }
        }
        return candidate is not null && VideoIdRegex().IsMatch(candidate) ? candidate : null;
    }
    [GeneratedRegex("^[a-zA-Z0-9_-]{11}$")]
    private static partial Regex VideoIdRegex();
}
