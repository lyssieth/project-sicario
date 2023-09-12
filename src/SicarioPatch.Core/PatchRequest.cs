using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MediatR;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed class PatchRequest : IRequest<FileInfo>
{
    public PatchRequest(IEnumerable<WingmanMod> mods)
    {
        Mods = mods.ToList().RebuildModList();
        Id = Guid.NewGuid().ToString("N");
    }

    [Obsolete("Only used for deserialization", true)]
    public PatchRequest()
    {
    }

    public required string Id { get; init; }

    [JsonInclude] public List<WingmanMod> Mods { get; private set; } = new();
    public bool PackResult { get; init; }

    public string? Name { get; init; }
    public Dictionary<string, string> TemplateInputs { get; set; } = new();

    public Dictionary<string, FileInfo> AdditionalFiles { get; set; } = new();

    public string? UserName { get; init; }
}