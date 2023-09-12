using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SicarioPatch.Integration;

namespace SicarioPatch.Loader;

internal sealed class LocalGameFinder : IGameSource
{
    private readonly string _rootPath;

    public LocalGameFinder() : this(AppContext.BaseDirectory)
    {
    }

    public LocalGameFinder(string rootPath)
    {
        _rootPath = rootPath;
    }

    private static Dictionary<string, Func<DirectoryInfo, DirectoryInfo?>> Candidates => new()
    {
        ["Project Wingman"] = static di => di.GetFiles("ProjectWingman.exe").Any() ? di : null,
        ["ProjectWingman"] = static di =>
            di.Parent != null && di.Parent.GetFiles("ProjectWingman.exe").Any() ? di.Parent : null,
        ["Paks"] = static di => di.Parent?.Parent?.Parent,
        ["~mods"] = static di => di.Parent?.Parent?.Parent?.Parent
    };

    public string? GetGamePath()
    {
        var di = new DirectoryInfo(_rootPath);
        if (!di.Exists || !Candidates.ContainsKey(di.Name)) return null;

        var targetDir = Candidates[di.Name].Invoke(di);
        return targetDir?.FullName;
    }

    public string? GetGamePakPath()
    {
        var dir = GetGamePath();
        if (dir == null) return null;

        var pakFilePath = Path.Join(dir, "ProjectWingman", "Content", "Paks",
            "ProjectWingman-WindowsNoEditor.pak");
        return File.Exists(pakFilePath) ? pakFilePath : null;
    }
}