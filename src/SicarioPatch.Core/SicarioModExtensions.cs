using System;
using System.Collections.Generic;
using System.Linq;
using ModEngine.Core;
using SicarioPatch.Engine;

namespace SicarioPatch.Core;

public static class SicarioModExtensions
{
    public static List<string> GetFilesModified(this SicarioMod mod)
    {
        // var fp = mod.FilePatches?.Keys.ToList();
        // ReSharper disable once CollectionNeverUpdated.Local - original implementation commented out, but left in for reference
        var fp = new List<string>();
        var ap = mod.AssetPatches?.Keys.ToList();
        return (ap ?? throw new InvalidOperationException()).Concat(fp).Distinct().ToList();
    }

    public static int GetPatchCount(this SicarioMod mod)
    {
        // var allFilePatches = mod.FilePatches.SelectMany(fp => fp.Value).SelectMany(ps => ps.Patches).Count();
        // ReSharper disable once ConvertToConstant.Local - original implementation commented out, but left in for reference
        var allFilePatches = 0;
        var allPatches = mod.AssetPatches.SelectMany(static fp => fp.Value).SelectMany(static ps => ps.Patches).Count();
        // ReSharper disable once UselessBinaryOperation - original implementation commented out, but left in for reference
        return allFilePatches + allPatches;
    }

    public static IEnumerable<PatchParameter> WhereValid(this IEnumerable<PatchParameter> parameters)
    {
        return parameters.Where(static p => !string.IsNullOrWhiteSpace(p.Id));
    }

    public static IDictionary<string, string> FallbackToDefaults(this IDictionary<string, string> dict,
        IEnumerable<PatchParameter> parameters)
    {
        var patchParameters = parameters as PatchParameter[] ?? parameters.ToArray();
        if (dict.Any() && patchParameters.Any())
            return dict.Select(kv =>
            {
                if (string.IsNullOrWhiteSpace(kv.Value))
                    return patchParameters.FirstOrDefault(p => p.Id == kv.Key) is var matchingParam &&
                           matchingParam != null
                        ? new KeyValuePair<string, string>(kv.Key,
                            matchingParam.Default ?? throw new InvalidOperationException())
                        : kv;

                return kv;
            }).ToDictionary(static k => k.Key, static v => v.Value);

        return dict;
    }
}