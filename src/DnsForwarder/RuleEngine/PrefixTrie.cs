namespace DnsForwarder.RuleEngine;

internal sealed class PrefixTrie
{
    private sealed class Node
    {
        public Dictionary<string, Node> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CompiledRule> Rules { get; } = new();
    }

    private readonly Node _root = new();

    public void Add(string prefix, CompiledRule rule)
    {
        var labels = prefix.Split('.', StringSplitOptions.RemoveEmptyEntries);

        var node = _root;
        foreach (var l in labels)
        {
            if (!node.Children.TryGetValue(l, out var child))
            {
                child = new Node();
                node.Children[l] = child;
            }
            node = child;
        }

        node.Rules.Add(rule);
    }

    public IEnumerable<CompiledRule> MatchAll(string domain)
    {
        var labels = domain.Split('.', StringSplitOptions.RemoveEmptyEntries);

        var node = _root;
        var results = new List<CompiledRule>();

        foreach (var l in labels)
        {
            if (!node.Children.TryGetValue(l, out var child))
                break;

            node = child;

            if (node.Rules.Count > 0)
                results.AddRange(node.Rules);
        }

        return results;
    }
}
