using System.Collections.Generic;
using JetBrains.Annotations;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable. - Deserialized from JSON
namespace SicarioPatch.Core;

[PublicAPI]
public sealed class WingmanPreset
{
    public int Version { get; set; } = 1;
    public string? EngineVersion { get; set; }
    public Dictionary<string, string> ModParameters { get; set; }
    public List<WingmanMod> Mods { get; set; }
}