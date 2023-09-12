using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SicarioPatch.Core;

namespace SicarioPatch.App.Infrastructure;

public sealed class FileBuildLog : IBuildLog
{
    private readonly string? _logPath;

    private readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    private readonly ILogger<FileBuildLog> _logger;

    public FileBuildLog(IConfiguration config, ILogger<FileBuildLog> logger)
    {
        _logPath = config.GetValue<string?>("BuildLogPath", null);
        _logger = logger;
    }


    public void SaveRequest(PatchRequestSummary summary)
    {
        if (_logPath == null) return;

        try
        {
            var json = JsonSerializer.Serialize(summary, _jsonOpts);
            var targetPath = Path.Combine(_logPath, $"{summary.Id}.json");
            File.WriteAllText(targetPath, json);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to write summary for {id}!", summary.Id);
        }
    }
}