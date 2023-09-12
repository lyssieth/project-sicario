using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed record PatchRequestSummary(string Id)
{
    public DateTime BuildTime { get; } = DateTime.UtcNow;
    public List<string> IncludedPatches { get; init; } = new();
    public Dictionary<string, string> Inputs { get; init; } = new();
    public string? FileName { get; set; }
    public string? UserName { get; set; }
}