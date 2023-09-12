using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace SicarioPatch.Core;

public sealed class BuildLogBehaviour : IPipelineBehavior<PatchRequest, FileInfo>
{
    private readonly IBuildLog? _log;

    public BuildLogBehaviour()
    {
    }

    public BuildLogBehaviour(IBuildLog log)
    {
        _log = log;
    }

    public async Task<FileInfo> Handle(PatchRequest request, RequestHandlerDelegate<FileInfo> next,
        CancellationToken cancellationToken)
    {
        var requestId = request.Id;
        var result = await next();
        _log?.SaveRequest(new PatchRequestSummary(requestId)
        {
            Inputs = request.TemplateInputs,
            FileName = result.Name,
            IncludedPatches = request.Mods.Select(static m => m.GetLabel()).ToList(),
            UserName = request.UserName
        });
        return result;
    }
}