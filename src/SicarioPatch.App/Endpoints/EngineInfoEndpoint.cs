using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.ApiEndpoints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using SicarioPatch.Engine;

namespace SicarioPatch.App.Endpoints;

[PublicAPI]
public sealed class
    EngineInfoEndpoint : EndpointBaseAsync.WithoutRequest.WithActionResult<EngineInfoEndpoint.EngineInfoResponse>
{
    private readonly IEngineInfoProvider _engineInfoProvider;

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

    public EngineInfoEndpoint(IEngineInfoProvider engineInfoProvider)
    {
        _engineInfoProvider = engineInfoProvider;
    }

    [PublicAPI]
    public sealed class EngineInfoResponse
    {
        public required string EngineVersion { get; init; }
    }

    [HttpGet("/engineInfo")]
    public override Task<ActionResult<EngineInfoResponse>> HandleAsync(
        CancellationToken cancellationToken = new())
    {
        return Task.FromResult<ActionResult<EngineInfoResponse>>(new JsonResult(new EngineInfoResponse
            { EngineVersion = _engineInfoProvider.GetEngineVersion() ?? "unknown" }, _jsonOpts));
    }
}