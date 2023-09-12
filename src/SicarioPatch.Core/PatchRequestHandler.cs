using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using ModEngine.Build;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed class PatchRequestHandler : IRequestHandler<PatchRequest, FileInfo?>
{
    private readonly WingmanPatchServiceBuilder _builder;

    public PatchRequestHandler(WingmanPatchServiceBuilder servBuilder)
    {
        _builder = servBuilder;
    }

    private readonly Dictionary<string, IEnumerable<string>> _sideCars = new()
    {
        [".uexp"] = new[] { ".uasset" },
        [".uasset"] = new[] { ".uexp" }
        // [".umap"] = new[] {".uexp"}
    };

    public async Task<FileInfo?> Handle(PatchRequest request, CancellationToken cancellationToken)
    {
        using var mpServ = await _builder.GetPatchEngineService(request.Mods, request.Name);
        if (request.AdditionalFiles.Any())
            mpServ.PreBuildAction = context =>
            {
                foreach (var (relPath, file) in request.AdditionalFiles)
                    context.AddFile(Path.Combine(context.WorkingDirectory.GetDirectories().First().Name, relPath),
                        file);
#pragma warning disable CS8603 // Possible null reference return. - This is a delegate, it's fine.
                return null;
#pragma warning restore CS8603
            };

        await mpServ.LoadFiles(FileSelectors.SidecarFiles(_sideCars));
        // await mpServ.LoadAssetFiles(FileSelectors.SidecarFiles(_sideCars)).LoadFiles(FileSelectors.SidecarFiles(_sideCars));
        await mpServ.RunPatches();
        // await mpServ.RunAssetPatches();
        (bool Success, FileSystemInfo? Result)? result;
        if (request.PackResult)
            result = await mpServ.RunBuildAsync($"merged-{DateTime.UtcNow.Ticks}_P.pak");
        else
            result = mpServ.RunAction(static ctx => ctx.WorkingDirectory.ToZipFile());
        if (result is not { Success: true, Result: FileInfo info }) return null;

        var tempFi = new FileInfo(Path.Combine(Path.GetTempPath(), info.Name));
        info.MoveTo(tempFi.FullName);
        return tempFi.Exists ? tempFi : null;
    }
}