using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using ModEngine.Merge;
using SicarioPatch.Core;
using SicarioPatch.Engine;
using SicarioPatch.Integration;

namespace SicarioPatch.Loader.Providers;

public sealed class LoosePresetProvider : IMergeProvider<WingmanMod>
{
    private readonly IEngineInfoProvider _engineInfo;
    private readonly ILogger<LoosePresetProvider> _logger;

    public LoosePresetProvider(IEngineInfoProvider engineInfo, ILogger<LoosePresetProvider> logger)
    {
        _engineInfo = engineInfo;
        _logger = logger;
    }

    public string Name => "loosePresets";

    public IEnumerable<MergeComponent<WingmanMod>> GetMergeComponents(List<string>? searchPaths)
    {
        var presetPaths = (searchPaths ?? new List<string>())
            .Where(Directory.Exists)
            .SelectMany(static d => Directory.EnumerateFiles(d, "*.dtp", SearchOption.AllDirectories))
            .OrderBy(static f => f)
            .ToList();
        var presets = PresetFileLoader.LoadFromFiles(presetPaths);
        presets = presets.Where(p =>
        {
            var supported = p.Value.IsSupportedBy(_engineInfo);
            if (!supported)
                _logger.LogWarning(
                    "[bold]Incompatible embed![/] This preset is not supported by the current engine version and will not be loaded!");
            return supported;
        }).ToDictionary();

        var loosePresetInputs = presets
            .Select(static p => p.Value.ModParameters)
            .Aggregate(new Dictionary<string, string>(), static (total, next) => total.MergeLeft(next)
            );
        yield return new MergeComponent<WingmanMod>
        {
            Name = "loosePresets",
            Priority = 2,
            Parameters = loosePresetInputs,
            Message = $"[dodgerblue2]Loaded [bold]{presets.Count}[/] loose presets from file.[/]",
            Mods = presets.SelectMany(static p => p.Value.Mods),
            MergedResources = presets.ToDictionary(static k => k.Key,
                static v => string.Join(";", v.Value.Mods.Select(static m => m.GetLabel())))
        };
    }
}