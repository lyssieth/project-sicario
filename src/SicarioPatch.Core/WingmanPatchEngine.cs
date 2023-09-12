using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildEngine;
using Microsoft.Extensions.Logging;
using ModEngine.Build;
using ModEngine.Core;

namespace SicarioPatch.Core;

public sealed class WingmanPatchEngine : ModPatchService<WingmanMod, DirectoryBuildContext>
{
    // private readonly List<PatchEngineDefinition<Patch>> _patchEngines = new();
    // private IEnumerable<PatchEngineDefinition<Patch>> PatchEngines => _patchEngines.OrderBy(p => p.Priority);

    internal WingmanPatchEngine(List<WingmanMod> mods, DirectoryBuildContext context, ISourceFileService fileService,
        IModBuilder? modBuilder, IEnumerable<PatchEngineDefinition<WingmanMod, Patch>> patchEngineDefinitions,
        ILogger<WingmanPatchEngine>? logger) : base(mods, context, fileService, modBuilder, patchEngineDefinitions,
        logger)
    {
    }

    public override async Task<ModPatchService<WingmanMod, DirectoryBuildContext>> RunPatches()
    {
        foreach (var patchEngine in PatchEngines)
        foreach (var mod in Mods)
        {
            var modifiedFiles = new List<FileInfo>();
            Logger?.LogInformation("Running patches for {mod}", mod.GetLabel());
            var patches = patchEngine.PatchSelector(mod);
            foreach (var (targetFile, patchSets) in patches)
            {
                var srcFile = new SourceFile(targetFile);
                try
                {
                    var realFile = BuildContext.GetFile(targetFile);
                    if (realFile != null) srcFile.File = realFile;
                }
                catch
                {
                    // ignored
                }

                var patchSetList = patchSets.ToList();
                Logger?.LogDebug("Patching {fileName}...", Path.GetFileName(targetFile));
                var fi = await patchEngine.Engine.RunPatch(srcFile, patchSetList);
                modifiedFiles.AddRange(fi);
            }

            Logger?.LogDebug("Modified {modifiedCount} files: {modifiedFiles}", modifiedFiles.Count,
                string.Join(", ", modifiedFiles.Select(static f => f.Name)));
        }

        return this;
    }
}