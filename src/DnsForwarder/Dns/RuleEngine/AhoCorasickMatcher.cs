using System.Collections.Generic;

namespace DnsForwarder.Dns.RuleEngine;

internal sealed class AhoCorasickMatcher
{
    private sealed class Node
    {
        public Dictionary<char, Node> Next { get; } = new();
        public Node? Fail { get; set; }
        public List<CompiledRule> Output { get; } = new();
    }

    private readonly Node _root = new();

    public void Add(string pattern, CompiledRule rule)
    {
        var core = pattern.Trim('*');
        if (string.IsNullOrWhiteSpace(core))
            return;

        var node = _root;
        foreach (var c in core)
        {
            if (!node.Next.TryGetValue(c, out var child))
            {
                child = new Node();
                node.Next[c] = child;
            }
            node = child;
        }

        node.Output.Add(rule);
    }

    public void Build()
    {
        var queue = new Queue<Node>();

        foreach (var kv in _root.Next)
        {
            kv.Value.Fail = _root;
            queue.Enqueue(kv.Value);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var kv in current.Next)
            {
                var c = kv.Key;
                var child = kv.Value;

                var fail = current.Fail;
                while (fail != null && !fail.Next.ContainsKey(c))
                    fail = fail.Fail;

                child.Fail = fail?.Next.GetValueOrDefault(c) ?? _root;

                foreach (var r in child.Fail.Output)
                    child.Output.Add(r);

                queue.Enqueue(child);
            }
        }
    }

    public IEnumerable<CompiledRule> Match(string text)
    {
        var node = _root;

        foreach (var c in text)
        {
            while (node != null && !node.Next.ContainsKey(c))
                node = node.Fail;

            node ??= _root;

            if (node.Next.TryGetValue(c, out var next))
                node = next;

            foreach (var r in node.Output)
                yield return r;
        }
    }
}
