using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using SicarioPatch.Engine;

namespace SicarioPatch.App.Infrastructure;

[PublicAPI]
public sealed class ConfigurationEngineInfoProvider : IEngineInfoProvider
{
    private readonly IConfiguration _config;

    public ConfigurationEngineInfoProvider(IConfiguration config)
    {
        _config = config;
    }

    public string? GetEngineVersion()
    {
        var requestEmbed = _config.GetValue("EngineVersion", GetFallbackVersion() ?? string.Empty);
        return requestEmbed;
    }

    private static string? GetFallbackVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly == null) return null;

        var infoVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        return infoVersion;
    }
}