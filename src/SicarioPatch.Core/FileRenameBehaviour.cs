using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildEngine;
using MediatR;

namespace SicarioPatch.Core;

public sealed class FileRenameBehaviour : IPipelineBehavior<PatchRequest, FileInfo>
{
    public async Task<FileInfo> Handle(PatchRequest request, RequestHandlerDelegate<FileInfo> next,
        CancellationToken cancellationToken)
    {
        var result = await next();
        if (string.IsNullOrWhiteSpace(request.Name)) return result;

        var targetFileName = Path.GetFileNameWithoutExtension(request.Name.MakeSafe());
        targetFileName = result.Extension == ".pak" && targetFileName.EndsWith("_P", StringComparison.Ordinal)
            ? targetFileName
            : targetFileName + "_P";
        if (result.Directory?.FullName == null) return result;

        var targetFilePath = Path.Combine(result.Directory?.FullName!, targetFileName + result.Extension);
        result.MoveTo(targetFilePath, true);
        return new FileInfo(targetFilePath);
    }
}