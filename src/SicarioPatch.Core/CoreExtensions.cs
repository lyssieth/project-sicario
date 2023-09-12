using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using JetBrains.Annotations;
using ModEngine.Core;

namespace SicarioPatch.Core;

[PublicAPI]
public static class CoreExtensions
{
    internal static string ToArgument(this string path)
    {
        return path.Contains(' ')
            ? $"\"{path}\""
            : path;
    }

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> pairs) where TKey : notnull
    {
        return pairs.ToDictionary(static k => k.Key, static v => v.Value);
    }

    public static IEnumerable<Patch> GetFilePatches(this WingmanMod mod)
    {
        var allPatches = mod.FilePatches.SelectMany(static fp => fp.Value).SelectMany(static ps => ps.Patches);
        return allPatches;
    }

    public static string? GetParentDirectoryPath(this FileInfo fi)
    {
        return fi.Directory?.FullName ?? Path.GetDirectoryName(fi.FullName);
    }

    //this is a horrible hack but otherwise the build process mutates the original mod selection
    internal static List<WingmanMod> RebuildModList(this List<WingmanMod> sourceList)
    {
        var json = JsonSerializer.Serialize(sourceList);
        return JsonSerializer.Deserialize<List<WingmanMod>>(json) ?? new List<WingmanMod>();
    }
}