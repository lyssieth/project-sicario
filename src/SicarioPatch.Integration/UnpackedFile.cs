// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace SicarioPatch.Integration;

internal sealed record UnpackedFile
{
    public string? AssetPath { get; set; }
    public string? OutputPath { get; set; }
    public string? SourceIndexHash { get; set; }
}