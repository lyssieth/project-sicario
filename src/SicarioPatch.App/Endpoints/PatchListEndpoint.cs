using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.ApiEndpoints;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SicarioPatch.Core;

namespace SicarioPatch.App.Endpoints;

[PublicAPI]
public sealed class
    PatchListEndpoint : EndpointBaseAsync.WithoutRequest.WithActionResult<PatchListEndpoint.PatchListResponse>
{
    [PublicAPI]
    public sealed class PatchListResponse
    {
        public required IEnumerable<WingmanModRecord> Mods { get; set; }
    }

    private readonly IMediator _mediator;

    private readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public PatchListEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("/mods")]
    public override async Task<ActionResult<PatchListResponse>> HandleAsync(CancellationToken cancellationToken = new())
    {
        var req = new ModsRequest { IncludePrivate = false, OnlyOwnMods = false };
        var res = await _mediator.Send(req, cancellationToken);
        var patches = res.Values.Select(static v => new WingmanModRecord(v)).ToList();
        return new JsonResult(new PatchListResponse { Mods = patches }, _jsonOpts);
    }
}