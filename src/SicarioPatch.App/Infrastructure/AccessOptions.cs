using System.Collections.Generic;

namespace SicarioPatch.App.Infrastructure;

public sealed class AccessOptions
{
    public List<string> AllowedUsers { get; set; } = new();

    public List<string> AllowedUploaders { get; set; } = new();
}