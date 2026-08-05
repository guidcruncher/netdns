using System.Text.RegularExpressions;

namespace DnsForwarder.Filtering;

public static class AdGuardRuleParser
{
    private static readonly Regex HostsRegex =
        new(@"^(0\.0\.0\.0|127\.0\.0\.1)\s+([a-zA-Z0-9\.-]+)$");

    public static ParsedRule? Parse(string raw, string source)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        raw = raw.Trim();

        // Full-line comments
        if (raw.StartsWith("!")) return null;
        if (raw.StartsWith("#")) return null;
        if (raw.StartsWith("//")) return null;
        if (raw.StartsWith(";")) return null;

        // Remove inline comments
        int commentIndex = raw.IndexOf('#');
        if (commentIndex >= 0)
            raw = raw[..commentIndex].Trim();

        commentIndex = raw.IndexOf('!');
        if (commentIndex >= 0)
            raw = raw[..commentIndex].Trim();

        commentIndex = raw.IndexOf("//");
        if (commentIndex >= 0)
            raw = raw[..commentIndex].Trim();

        commentIndex = raw.IndexOf(';');
        if (commentIndex >= 0)
            raw = raw[..commentIndex].Trim();

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Adblock-style: ||example.com^
        if (raw.StartsWith("||"))
        {
            var domain = raw[2..].TrimEnd('^');
            var pattern = new Regex($@"(^|\.){Regex.Escape(domain)}$", RegexOptions.IgnoreCase);

            return new ParsedRule
            {
                Source = source,
                Raw = raw,
                Pattern = pattern
            };
        }

        // Hosts-style: 0.0.0.0 ads.example.com
        var hostsMatch = HostsRegex.Match(raw);
        if (hostsMatch.Success)
        {
            var domain = hostsMatch.Groups[2].Value;
            var pattern = new Regex($"^{Regex.Escape(domain)}$", RegexOptions.IgnoreCase);

            return new ParsedRule
            {
                Source = source,
                Raw = raw,
                Pattern = pattern
            };
        }

        // Domains-only: ads.example.com
        if (raw.Contains('.'))
        {
            var pattern = new Regex($"^{Regex.Escape(raw)}$", RegexOptions.IgnoreCase);

            return new ParsedRule
            {
                Source = source,
                Raw = raw,
                Pattern = pattern
            };
        }

        return null;
    }
}
