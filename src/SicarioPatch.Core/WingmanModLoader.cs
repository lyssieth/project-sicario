﻿using System;
using System.Collections.Generic;
using ModEngine.Build;

namespace SicarioPatch.Core;

public sealed class WingmanModLoader : IModLoader<WingmanMod>
{
    public WingmanModLoader(IModLoader<WingmanMod> loader)
    {
        _modFileLoader = loader;
    }

    private readonly IModLoader<WingmanMod> _modFileLoader;

    public Dictionary<string, WingmanMod> LoadFromFiles(IEnumerable<string> filePaths,
        List<Func<WingmanMod, bool>>? loadRequirements = null)
    {
        var loadedMods = _modFileLoader.LoadFromFiles(filePaths);
        foreach (var (fileName, mod) in loadedMods)
        {
            mod.Id ??= System.IO.Path.GetFileNameWithoutExtension(fileName);
            if (mod.GetLabel(string.Empty).Contains("[TEST]")) mod.ModInfo.Unstable = true;
        }

        return loadedMods;
    }
}