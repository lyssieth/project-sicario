using System.Collections.Generic;
using JetBrains.Annotations;
using SicarioPatch.Core;

namespace SicarioPatch.App.Endpoints;

[PublicAPI]
public class PatchBuildSpecification
{
    public required Dictionary<string, string> InputParameters { get; set; }
    public required List<WingmanMod> Mods { get; set; }
    public required string OutputName { get; set; }
    public List<string>? IncludedMods { get; set; }
}