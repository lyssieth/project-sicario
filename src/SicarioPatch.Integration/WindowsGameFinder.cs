using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using NexusMods.Paths;

namespace SicarioPatch.Integration;

[SupportedOSPlatform("windows")]
public sealed class WindowsGameFinder : IGameSource
{
    private static string? LocateGamePath()
    {
        var steam = new SteamHandler(FileSystem.Shared, new WindowsRegistry());
        if (steam.FindAllGames().Any())
        {
            var game = steam.FindOneGameById(AppId.From(895870), out _);

            if (game != null) return game.Path.ToString();
        }

        var gog = new GOGHandler(new WindowsRegistry(), FileSystem.Shared);
        var gogGames = gog.FindAllGames().Where(static v => v.IsT0).Select(static v => v.AsT0).ToList();
        if (gogGames.Any() &&
            gogGames.FirstOrDefault(static g => g.Id == GOGGameId.From(1609812781)) is { } gogGame)
            return gogGame.Path.ToString();

        return null;
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
            Directory.GetFiles(pakPath, "ProjectWingman-WindowsNoEditor.pak").FirstOrDefault() is { } gamePak)
            return gamePak;

        return null;
    }
}