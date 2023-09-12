using JetBrains.Annotations;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed class ModBuildOptions
{
    public bool? PackResult { get; set; }
    public string? Name { get; set; }
    public string? UserIdentifier { get; set; }
}