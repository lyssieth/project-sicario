using System.Collections.Generic;
using System.IO;
using System.Linq;
using INIParser;
using Microsoft.Extensions.Configuration;
using ModEngine.Templating;

namespace SicarioPatch.App;

public sealed class IniModelProvider : ITemplateModelProvider
{
    public IniModelProvider(IConfiguration configuration)
    {
        FileName = configuration.GetValue("TemplateModelsFile", string.Empty);
    }

    private string? FileName { get; }

    public IEnumerable<ITemplateModel> LoadModels()
    {
        if (string.IsNullOrWhiteSpace(FileName) || new FileInfo(FileName) is not { Exists: true }) yield break;

        var file = new IniFile(FileName);
        foreach (var (section, keys) in file.Sections.Select(v => (v, file.GetKeys(v))))
            yield return new BasicTemplateModel
            {
                Name = section,
                Values = keys.ToDictionary(static k => k, v => file[section, v] ?? string.Empty)
            };
    }
}