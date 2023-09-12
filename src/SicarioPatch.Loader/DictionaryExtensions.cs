using System.Collections.Generic;
using System.Linq;

namespace SicarioPatch.Loader;

internal static class DictionaryExtensions
{
    // Works in C#3/VS2008:
    // Returns a new dictionary of this ... others merged leftward.
    // Keeps the type of 'this', which must be default-instantiable.
    // Example:
    //   result = map.MergeLeft(other1, other2, ...)
    public static T MergeLeft<T, TKey, TValue>(this T me, params IDictionary<TKey, TValue>[] others)
        where T : IDictionary<TKey, TValue>, new()
    {
        var newMap = new T();
        foreach (var p in new List<IDictionary<TKey, TValue>> { me }.Concat(others).SelectMany(static src => src))
            newMap[p.Key] = p.Value;
        return newMap;
    }
}