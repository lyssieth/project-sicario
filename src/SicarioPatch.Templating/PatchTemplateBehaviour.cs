using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModEngine.Templating;
using JetBrains.Annotations;
using MediatR;
using ModEngine.Core;
using SicarioPatch.Core;

namespace SicarioPatch.Templating;

[PublicAPI]
public sealed class PatchTemplateBehaviour : IPipelineBehavior<PatchRequest, FileInfo>
{
    private readonly TemplateService _template = new();


    public async Task<FileInfo> Handle(PatchRequest request, RequestHandlerDelegate<FileInfo> next,
        CancellationToken cancellationToken)
    {
        //var templateObj = DictionaryToObject(request.TemplateInputs.ToDictionary(k => k.Key, v => (object)v.Value));

        foreach (var mod in request.Mods)
        {
            var modelVars = _template.RenderVariables(request.TemplateInputs, mod.Variables);
            var dict = RenderFilePatchTemplates(request.TemplateInputs, mod, modelVars);

            mod.FilePatches = dict.Where(static kvp => kvp.Value.Any()).ToDictionary(static k => k.Key,
                static v =>
                {
                    return v.Value.Select(static ps => new PatchSet { Name = ps.Name, Patches = ps.Patches }).ToList();
                });
            var assetDict = RenderAssetPatchTemplates(request, mod, modelVars);
            mod.AssetPatches = assetDict;
        }

        return await next();
    }

    private Dictionary<string, List<PatchSet<Patch>>> RenderFilePatchTemplates(
        Dictionary<string, string> templateInputs, WingmanMod mod, Dictionary<string, string> modelVars)
    {
        var dict = mod.FilePatches.ToDictionary(static k => k.Key, kvp =>
        {
            var finalPatches = kvp.Value.Where(psList =>
            {
                // REMEMBER: this is to keep the step, so have to return false to skip it
                if (!mod.ModInfo.StepsEnabled.ContainsKey(psList.Name ?? string.Empty) ||
                    !_template.TryRender(mod.ModInfo.StepsEnabled[psList.Name ?? throw new InvalidOperationException()],
                        templateInputs, modelVars,
                        out var rendered)) return true;

                var result = !bool.TryParse(rendered, out var skip) || skip;
                // var result = bool.TryParse(rendered, out var skip) || skip;
                // do NOT invert result: result *is* inverted
                return result;
                // ReSharper disable once HeapView.ImplicitCapture - sad
            }).Select(psList => _template.RenderPatch(
                new PatchSet<Patch> { Name = psList.Name, Patches = psList.Patches },
                templateInputs, modelVars)).ToList();
            return finalPatches;
        });
        return dict;
    }

    private Dictionary<string, List<PatchSet<Patch>>> RenderAssetPatchTemplates(PatchRequest request, WingmanMod mod,
        Dictionary<string, string> modelVars)
    {
        var dict = mod.AssetPatches.ToDictionary(static k => k.Key, kvp =>
        {
            var finalPatches = kvp.Value.Where(psList =>
            {
                // REMEMBER: this is to keep the step, so have to return false to skip it
                if (psList.Name == null || !mod.ModInfo.StepsEnabled.ContainsKey(psList.Name) ||
                    !_template.TryRender(mod.ModInfo.StepsEnabled[psList.Name], request.TemplateInputs, modelVars,
                        out var rendered)) return true;

                var result = !bool.TryParse(rendered, out var skip) || skip;
                // var result = bool.TryParse(rendered, out var skip) || skip;
                // do NOT invert result: result *is* inverted
                return result;
                // ReSharper disable once HeapView.ImplicitCapture - sad
            }).Select(psList => _template.RenderPatch(psList, request.TemplateInputs, modelVars)).ToList();
            return finalPatches;
        });
        return dict;
    }
}