using System.Text.Json.Serialization;
using SicarioPatch.Core;

namespace SicarioPatch.Integration;

public sealed class EmbeddedRequest
{
    [JsonPropertyName("request")] public PatchRequest? Request { get; set; }
}