﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SicarioPatch.Core;

public static class PresetFileLoader
{
    public static Dictionary<string, WingmanPreset> LoadFromFiles(IEnumerable<string>? filePaths)
    {
        var fileMods = new Dictionary<string, WingmanPreset>();
        if (filePaths is null) return fileMods;

        foreach (var file in filePaths.Where(static f => f.Length > 0 && File.ReadAllText(f).Any()))
            try
            {
                // _logger?.LogTrace($"Attempting to load mod data from {file}");
                var allText = File.ReadAllText(file);
                var jsonOpts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    Converters =
                    {
                        new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                    }
                };
                if (JsonSerializer.Deserialize<WingmanPreset>(allText, jsonOpts) is { Mods: not null } jsonPreset)
                    // _logger?.LogTrace($"Successfully loaded mod data from {file}: {jsonMod.GetLabel(Path.GetFileName(file))}");
                    fileMods.Add(file, jsonPreset);
            }
            catch (System.Exception)
            {
                // _logger?.LogWarning($"Failed to load mod data from {file}!");
            }

        return fileMods;
    }
}