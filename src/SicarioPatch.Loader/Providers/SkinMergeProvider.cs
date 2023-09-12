using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModEngine.Merge;
using SicarioPatch.Core;
using SicarioPatch.Integration;

namespace SicarioPatch.Loader.Providers;

public sealed class SkinMergeProvider : IMergeProvider<WingmanMod>
{
    private readonly SkinSlotLoader _slotLoader;

    public SkinMergeProvider(SkinSlotLoader slotLoader)
    {
        _slotLoader = slotLoader;
    }

    public string Name => "customSkins";

    public IEnumerable<MergeComponent<WingmanMod>> GetMergeComponents(List<string>? searchPaths)
    {
        var skinPaths = _slotLoader.GetSkinPaths();
        var slotPatches = _slotLoader.GetSlotPatches(skinPaths).ToList();
        var slotLoader = _slotLoader.GetSlotMod(slotPatches);
        if (slotLoader == null) return System.Array.Empty<MergeComponent<WingmanMod>>();

        var skinSet = skinPaths
            .ToDictionary(static k => k.Key,
                static v => v.Value.Select(static p => Path.ChangeExtension(p, null)).Distinct().ToList())
            .ToDictionary(static k => k.Key, static v => string.Join(";", v.Value));
        return new[]
        {
            new MergeComponent<WingmanMod>
            {
                Name = "customSkins",
                Mods = new[] { slotLoader },
                MergedResources = skinSet,
                Message =
                    $"[dodgerblue2]Successfully compiled skin merge with [bold]{slotLoader.GetPatchCount()}[/] patches.[/]"
            }
        };
    }
}