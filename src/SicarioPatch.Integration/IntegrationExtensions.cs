using System.Collections.Generic;
using System.IO;
using System.Linq;
using SemanticVersioning;
using SicarioPatch.Core;
using SicarioPatch.Engine;
using UnPak.Core;

namespace SicarioPatch.Integration;

public static class IntegrationExtensions
{
    internal static string? GetGamePath(this IEnumerable<IGameSource> sources)
    {
        return sources.Select(static gs => gs.GetGamePath())
            .FirstOrDefault(static gp => !string.IsNullOrWhiteSpace(gp));
    }

    internal static string? GetGamePakPath(this IEnumerable<IGameSource> sources)
    {
        var pakPath = sources.Select(static gs => gs.GetGamePakPath())
            .FirstOrDefault(static gp => !string.IsNullOrWhiteSpace(gp));
        return pakPath;
    }

    internal static string GetVirtualPath(this Record r, PakFile pakFile)
    {
        return Path.Join(pakFile.MountPoint, r.FileName)
            .Replace(string.Join("/", "..", "..", ".."), string.Empty).TrimStart('/');
    }

    internal static string TrimPathTo(this string path, string pathSegment, string separator = "/")
    {
        var finalParts = path.Split(separator).SkipWhile(s => !string.Equals(s, pathSegment)).ToList();
        return finalParts.Any() ? string.Join("/", finalParts) : path;
    }

    public static bool IsSupportedBy(this WingmanPreset preset, IEngineInfoProvider engineInfo)
    {
        var presetRequested = preset.EngineVersion;
        var engineVersion = engineInfo.GetEngineVersion();
        if (string.IsNullOrWhiteSpace(presetRequested) || string.IsNullOrWhiteSpace(engineVersion)) return true;

        if (!Version.TryParse(engineVersion, true, out var engineVersionInfo) ||
            !Version.TryParse(presetRequested, true, out var presetVersionInfo)) return false;

        var supported = engineVersionInfo == Version.Parse("0.0.0") || engineVersionInfo >= presetVersionInfo;
        return supported;
    }
}