using System.Collections.Generic;
using JetBrains.Annotations;
using MediatR;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed class ModsRequest : IRequest<Dictionary<string, WingmanMod>>
{
    public string? UserName { get; set; }
    public bool IncludePrivate { get; set; }
    public bool OnlyOwnMods { get; set; }
}