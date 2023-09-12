using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace SicarioPatch.Integration;

[SupportedOSPlatform("linux")]
public class LinuxGameFinder : IGameSource
{
    private static string? LocateGamePath()
    {
        var steam = Path.Combine("/home", Environment.UserName, ".steam/root/steamapps/common/Project Wingman");

        return Directory.Exists(steam) ? steam : null;
    }

    public string? GetGamePath()
    {
        return LocateGamePath();
    }

    public string? GetGamePakPath()
    {
        var gamePath = LocateGamePath();
        if (gamePath == null) return null;

        var pakPath = Path.Join(gamePath, "ProjectWingman", "Content", "Paks");
        if (Directory.Exists(pakPath) &&
            Directory.GetFiles(pakPath, "ProjectWingman-LinuxNoEditor.pak").FirstOrDefault() is { } gamePak)
            return gamePak;

        return null;
    }
}