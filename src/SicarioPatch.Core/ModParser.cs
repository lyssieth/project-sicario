using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using JetBrains.Annotations;
using ModEngine.Core;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed class ModParser
{
    public string ToJson(Mod mod)
    {
        return JsonSerializer.Serialize(mod, Options);
    }

    public WingmanMod? ParseMod(string rawJson)
    {
        return JsonSerializer.Deserialize<WingmanMod>(rawJson, Options);
    }

    public static bool IsValid(WingmanMod mod)
    {
        return mod is { FilePatches: not null } && (mod.FilePatches.Any() ||
                                                    (mod.AssetPatches != null &&
                                                     mod.AssetPatches.Any()));
    }

    public JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public JsonSerializerOptions RelaxedOptions => new(Options)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}