using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using ModEngine.Templating;

namespace SicarioPatch.App;

public sealed class ConfigModelProvider : ITemplateModelProvider
{
    public ConfigModelProvider(IConfiguration configuration)
    {
        Section = configuration.GetSection("TemplateModels");
    }

    private IConfigurationSection Section { get; set; }

    public IEnumerable<ITemplateModel> LoadModels()
    {
        if (!Section.Exists()) yield break;

        var keys = Section.GetChildren();
        foreach (var modelKey in keys)
            yield return new BasicTemplateModel
            {
                Name = modelKey.Key,
                Values = modelKey.Get<Dictionary<string, string>>() ?? new Dictionary<string, string>()
            };
    }
}