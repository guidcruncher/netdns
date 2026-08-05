namespace DnsForwarder.RuleEngine;

internal sealed class WildcardBuckets
{
    private readonly Dictionary<string, List<CompiledRule>> _buckets =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string pattern, CompiledRule rule)
    {
        var core = pattern.Trim('*');
        if (!_buckets.TryGetValue(core, out var list))
        {
            list = new List<CompiledRule>();
            _buckets[core] = list;
        }

        list.Add(rule);
    }

    public IEnumerable<CompiledRule> MatchAll(string domain)
    {
        foreach (var kvp in _buckets)
        {
            if (domain.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                foreach (var r in kvp.Value)
                    yield return r;
        }
    }
}
