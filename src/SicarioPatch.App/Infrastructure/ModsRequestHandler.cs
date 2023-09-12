using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ModEngine.Core;
using SicarioPatch.Core;

namespace SicarioPatch.App.Infrastructure;

[PublicAPI]
internal sealed record ModUploadRequest : IRequest<string>
{
    public required string FileName { get; init; }
    public required Mod Mod { get; init; }
    public string? UploadTarget { get; init; }
}

[PublicAPI]
internal sealed record ModDeleteRequest : IRequest<bool>
{
    public required string FileName { get; init; }
}

[PublicAPI]
public sealed class ModsRequestHandler : IRequestHandler<ModsRequest, Dictionary<string, WingmanMod>>
{
    private WingmanModLoader _loader;
    private ModLoadOptions? _fileOpts;
    private bool _enableTesting;

    public ModsRequestHandler(WingmanModLoader loader, ModLoadOptions? loadOpts, IWebHostEnvironment env)
    {
        _loader = loader;
        _fileOpts = loadOpts;
        _enableTesting = env.IsDevelopment();
    }

    public Task<Dictionary<string, WingmanMod>> Handle(ModsRequest request, CancellationToken cancellationToken)
    {
        var allFiles = new List<string>();
        foreach (var localFiles in from sourcePath in _fileOpts?.Sources ?? new List<string>()
                 select Directory.EnumerateFiles(sourcePath, _fileOpts?.Filter ?? "*", SearchOption.TopDirectoryOnly))
            allFiles.AddRange(localFiles);

        var fileMods = _loader.LoadFromFiles(allFiles)
            .ToDictionary(static k => Path.GetFileName(k.Key), static v => v.Value);
        var allMods = request.OnlyOwnMods && !string.IsNullOrWhiteSpace(request.UserName)
            ? fileMods.Where(static m => !m.Value.ModInfo.Private).Where(MatchesAuthor(request.UserName))
            : fileMods.Where(static m => !m.Value.ModInfo.Private);
        if (request.IncludePrivate && !string.IsNullOrWhiteSpace(request.UserName))
        {
            var privateMods = fileMods.Where(static m => m.Value.ModInfo.Private)
                .Where(MatchesAuthor(request.UserName));
            allMods = allMods.Concat(privateMods);
        }

        allMods = allMods.Where(m => _enableTesting || !GetName(m.Value, string.Empty).Contains("[TEST]"));

        return Task.FromResult(allMods.ToDictionary());
    }

    private static Func<KeyValuePair<string, WingmanMod>, bool> MatchesAuthor(string author)
    {
        return modPair =>
        {
            var mod = modPair.Value;
            return mod.Metadata != null && mod.Metadata.Author == author;
        };
    }

    private static string GetName(Mod? mod, string defaultValue = null!)
    {
        var name = mod?.Metadata?.DisplayName;
        return string.IsNullOrWhiteSpace(name) ? defaultValue : name;
    }
}