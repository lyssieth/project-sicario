﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModEngine.Core;
using SicarioPatch.Core;
using UnPak.Core;

namespace SicarioPatch.Integration;

public sealed class SkinSlotLoader
{
    private readonly IEnumerable<IGameSource> _gameSources;
    private readonly PakFileProvider _pakFileProvider;

    public SkinSlotLoader(IEnumerable<IGameSource> gameSources, PakFileProvider pakFileProvider)
    {
        _gameSources = gameSources;
        _pakFileProvider = pakFileProvider;
    }

    public Dictionary<string, List<string>> GetSkinPaths()
    {
        var pakPath = _gameSources.GetGamePakPath();
        var additionalSkins = new List<string>();
        if (pakPath == null)
            return additionalSkins.Any()
                ? additionalSkins.GroupBy(static a => new FileInfo(a).Directory?.Name ?? string.Empty)
                    .ToDictionary(static g => g.Key, static g => g.ToList())
                : new Dictionary<string, List<string>>();

        var pakFiles = new FileInfo(pakPath).Directory!.EnumerateFiles("*_P.pak", SearchOption.AllDirectories);
        foreach (var file in pakFiles)
            try
            {
                var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var reader = _pakFileProvider.GetReader(fs);
                var pakFile = reader.ReadFile();
                var addSkins = pakFile.Records.Select(a => a.GetVirtualPath(pakFile)).Where(static r =>
                    r.StartsWith("ProjectWingman/Content/Assets/Skins", StringComparison.Ordinal));
                additionalSkins.AddRange(addSkins);
            }
            catch
            {
                //ignored
            }

        return additionalSkins.Any()
            ? additionalSkins.GroupBy(static a => new FileInfo(a).Directory?.Name ?? string.Empty)
                .ToDictionary(static g => g.Key, static g => g.ToList())
            : new Dictionary<string, List<string>>();
    }

    public IEnumerable<PatchSet> GetSlotPatches(Dictionary<string, List<string>>? skinPaths = null)
    {
        var skins = skinPaths ?? GetSkinPaths();
        foreach (var (aircraft, paths) in skins)
        {
            var assetPaths = paths.Where(static p => Path.GetExtension(p) == ".uasset").ToList();
            yield return new PatchSet()
            {
                Name = $"Add {assetPaths.Count} {aircraft}",
                Patches = assetPaths.Select(p => new Patch
                {
                    Type = "objectRef",
                    Template = $"datatable:['{aircraft}'].{{'SkinLibraryLegacy*'}}",
                    Value =
                        $"'{Path.GetFileNameWithoutExtension(p)}':'/Game/{Path.ChangeExtension(p.TrimPathTo("Assets"), null)}'"
                }).ToList()
            };
        }
    }

    public WingmanMod? GetSlotMod(IEnumerable<PatchSet<Patch>>? patchSets = null)
    {
        var assetPatches = (patchSets ?? GetSlotPatches()).ToList();
        return assetPatches.Any()
            ? new WingmanMod
            {
                Id = "skinSlots",
                FilePatches = new Dictionary<string, List<PatchSet>>(),
                AssetPatches = new Dictionary<string, List<PatchSet<Patch>>>
                {
                    ["ProjectWingman/Content/ProjectWingman/Blueprints/Data/AircraftData/DB_Aircraft.uexp"] =
                        (patchSets ?? GetSlotPatches()).ToList()
                }
            }
            : null;
    }
}