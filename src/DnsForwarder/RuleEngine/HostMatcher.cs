using System.Net;

namespace DnsForwarder.RuleEngine;

internal sealed class HostMatcher
{
    private enum HostPatternKind
    {
        Exact,
        Suffix,
        Prefix,
        WildcardSubstring
    }

    private sealed record HostPattern(
        string Pattern,
        string Core,
        IPAddress Address,
        int Specificity,
        HostPatternKind Kind);

    private readonly List<HostPattern> _patterns = new();

    public void Add(string host, IPAddress ip)
    {
        var pattern = host.Trim();
        if (string.IsNullOrWhiteSpace(pattern))
            return;

        // Classification similar to rule engine, but simplified for hosts
        if (!pattern.Contains('*'))
        {
            // Exact host
            var core = pattern.ToLowerInvariant();
            _patterns.Add(new HostPattern(pattern, core, ip, core.Length, HostPatternKind.Exact));
            return;
        }

        if (pattern.StartsWith("*."))
        {
            // Suffix wildcard: *.example.com
            var core = pattern[2..].ToLowerInvariant();
            _patterns.Add(new HostPattern(pattern, core, ip, core.Length, HostPatternKind.Suffix));
            return;
        }

        if (pattern.EndsWith(".*") && !pattern.StartsWith("*."))
        {
            // Prefix wildcard: example.*
            var core = pattern[..^2].ToLowerInvariant();
            _patterns.Add(new HostPattern(pattern, core, ip, core.Length, HostPatternKind.Prefix));
            return;
        }

        // General wildcard substring: *ads*, *tracking*, *cdn*.example.com
        var trimmed = pattern.Trim('*').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        _patterns.Add(new HostPattern(pattern, trimmed, ip, trimmed.Length, HostPatternKind.WildcardSubstring));
    }

    public IPAddress? MatchMostSpecific(string domain)
    {
        if (_patterns.Count == 0)
            return null;

        var lower = domain.ToLowerInvariant();

        HostPattern? best = null;

        foreach (var p in _patterns)
        {
            if (!IsMatch(p, lower))
                continue;

            if (best == null || p.Specificity > best.Specificity)
                best = p;
        }

        return best?.Address;
    }

    private static bool IsMatch(HostPattern p, string domain)
    {
        return p.Kind switch
        {
            HostPatternKind.Exact =>
                string.Equals(domain, p.Core, StringComparison.OrdinalIgnoreCase),

            HostPatternKind.Suffix =>
                domain.EndsWith(p.Core, StringComparison.OrdinalIgnoreCase),

            HostPatternKind.Prefix =>
                domain.StartsWith(p.Core, StringComparison.OrdinalIgnoreCase),

            HostPatternKind.WildcardSubstring =>
                domain.Contains(p.Core, StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }
}
