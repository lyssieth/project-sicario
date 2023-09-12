using System.Collections.Generic;
using System.Text.Json.Serialization;
using ModEngine.Core;
using SicarioPatch.Engine;

namespace SicarioPatch.Core;

public sealed class WingmanMod : SicarioMod
{
    public Dictionary<string, List<PatchSet>> FilePatches { get; set; } = new();

    // ReSharper disable once CollectionNeverUpdated.Global - Deserialized
    [JsonPropertyName("_inputs")] public List<PatchParameter> Parameters { get; set; } = new();
}