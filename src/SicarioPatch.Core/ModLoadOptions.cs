using System.Collections.Generic;
using JetBrains.Annotations;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed class ModLoadOptions
{
    public List<string> Sources { get; set; } = new();
    public string Filter { get; set; } = "*.dtm";
}