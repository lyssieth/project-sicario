using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace SicarioPatch.Integration;

[PublicAPI]
public sealed class ConfigurationGameSource : IGameSource
{
    private readonly IConfiguration _config;

    public ConfigurationGameSource(IConfiguration config)
    {
        _config = config;
    }

    public string? GetGamePath()
    {
        return _config.GetChildren().FirstOrDefault(static c => c.Key == "GamePath")?.Value;
    }

    public string? GetGamePakPath()
    {
        return _config.GetChildren().FirstOrDefault(static c => c.Key == "GamePakPath")?.Value;
    }
}