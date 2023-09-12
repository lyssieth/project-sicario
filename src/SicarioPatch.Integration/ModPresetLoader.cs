using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using JetBrains.Annotations;
using SicarioPatch.Core;
using UnPak.Core;

namespace SicarioPatch.Integration;

[PublicAPI]
public sealed class ModPresetLoader
{
    private readonly IEnumerable<IGameSource> _gameSources;
    private readonly PakFileProvider _pakFileProvider;
    private readonly ModParser _parser;

    public ModPresetLoader(IEnumerable<IGameSource> gameSources, PakFileProvider pakFileProvider, ModParser parser)
    {
        _gameSources = gameSources;
        _pakFileProvider = pakFileProvider;
        _parser = parser;
    }

    public IEnumerable<WingmanPreset> LoadFromMods()
    {
        var allPaks = GetAllMods();
        var builtMods = new List<WingmanPreset>();
        foreach (var pakFileInfo in allPaks)
            try
            {
                var reader = _pakFileProvider.GetReader(pakFileInfo.OpenRead());
                var file = reader.ReadFile();
                var requestFile =
                    file.Records.FirstOrDefault(r =>
                        r.GetVirtualPath(file).Contains("sicario") &&
                        Path.GetExtension(r.GetVirtualPath(file)) == ".dtp");
                if (requestFile == null) continue;

                var outSt = requestFile.Unpack(file.FileStream);
                var request = new StreamReader(outSt).ReadToEnd();
                var embed = JsonSerializer.Deserialize<WingmanPreset>(request, _parser.Options);
                if (embed?.Mods != null && embed.Mods.Any()) builtMods.Add(embed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //ignored
            }

        return builtMods;
    }

    private IEnumerable<FileInfo> GetAllMods()
    {
        var pakPath = _gameSources.Select(static gs => gs.GetGamePakPath())
            .FirstOrDefault(static gp => !string.IsNullOrWhiteSpace(gp));
        if (pakPath == null) return new List<FileInfo>();

        var pakRootPath = new FileInfo(pakPath).GetParentDirectoryPath();
        Debug.Assert(pakRootPath != null, nameof(pakRootPath) + " != null");
        var allPaks = Directory.EnumerateFiles(pakRootPath, "*.pak", SearchOption.AllDirectories);
        return allPaks.Where(static p => Path.GetFileNameWithoutExtension(p) != "ProjectWingman-WindowsNoEditor")
            .Where(static p => new FileInfo(p).Directory?.Name != "~sicario").Select(static p => new FileInfo(p));
    }
}