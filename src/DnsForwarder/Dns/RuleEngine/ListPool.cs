using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DnsForwarder.Utils
{
    internal static class ListPool<T>
    {
        private static readonly ConcurrentBag<List<T>> _bag = new();

        public static List<T> Rent()
        {
            if (_bag.TryTake(out var l))
            {
                l.Clear();
                return l;
            }

            return new List<T>();
        }

        public static void Return(List<T> list)
        {
            list.Clear();
            _bag.Add(list);
        }
    }
}
