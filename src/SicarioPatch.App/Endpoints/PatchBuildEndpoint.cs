using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.ApiEndpoints;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ModEngine.Build;
using SicarioPatch.Core;

namespace SicarioPatch.App.Endpoints;

[PublicAPI]
public sealed class
    PatchBuildEndpoint : EndpointBaseAsync.WithRequest<PatchBuildEndpoint.PatchBuildEndpointRequest>.WithoutResult
{
    [PublicAPI]
    public class PatchBuildEndpointRequest
    {
        [FromBody] public required PatchBuildSpecification BuildSpecification { get; set; }

        [FromQuery] public bool AutoPack { get; set; } = true;
    }

    private readonly IMediator _mediator;
    private readonly ModFileLoader<WingmanMod> _modFileLoader;

    public PatchBuildEndpoint(IMediator mediator, ModFileLoader<WingmanMod> modFileLoader)
    {
        _mediator = mediator;
        _modFileLoader = modFileLoader;
    }

    [HttpPost("/mods/build")]
    public override async Task<ActionResult> HandleAsync([FromRoute] PatchBuildEndpointRequest request,
        CancellationToken cancellationToken = new())
    {
        // ReSharper disable once CollectionNeverQueried.Local
        var buildMods = new List<WingmanMod?>();
        var spec = request.BuildSpecification;
        if (spec.IncludedMods != null && spec.IncludedMods.Any())
        {
            var allLoaded = await _mediator.Send(new ModsRequest { IncludePrivate = false }, cancellationToken);
            var includedMods = spec.IncludedMods.Select(s => allLoaded.Values.FirstOrDefault(v => v.Id == s))
                .Where(static m => m != null).ToList();
            buildMods.AddRange(includedMods);
        }

        var inputParams = spec.InputParameters;
        var req = new PatchRequest(spec.Mods)
        {
            Id = string.Empty,
            PackResult = request.AutoPack,
            TemplateInputs = inputParams,
            Name = string.IsNullOrWhiteSpace(spec.OutputName) ? null : spec.OutputName
        };
        var res = await _mediator.Send(req, cancellationToken);
        var outName = res.Name;
        var memFile = new MemoryStream(await System.IO.File.ReadAllBytesAsync(res.FullName, cancellationToken));
        res.Delete();
        return File(memFile, res.Extension == ".zip" ? "application/zip" : "application/octet-stream", outName);
    }
}