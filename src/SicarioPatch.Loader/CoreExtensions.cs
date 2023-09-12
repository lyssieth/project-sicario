using System.Diagnostics;
using System.IO;
using Spectre.Console.Cli;

namespace SicarioPatch.Loader;

internal static class CoreExtensions
{
    internal static void EnsureDirectoryExists(this string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        if (!Directory.Exists(path))
            throw new UnreachableException($"Failed to create directory {path}");
    }

    internal static bool Is(this FlagValue<bool> flagValue, bool target, bool defaultValue = false)
    {
        return flagValue.IsSet ? flagValue.Value == target : defaultValue == target;
    }
}