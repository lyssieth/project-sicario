using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildEngine;
using HexPatch;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ModEngine.Build;
using ModEngine.Build.Diagnostics;
using ModEngine.Core;
using SicarioPatch.Engine;
using Patch = ModEngine.Core.Patch;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed class WingmanPatchService : ModPatchService<WingmanMod, DirectoryBuildContext>
{
    private readonly AssetPatcher _assetPatcher;
    private readonly FilePatcher _filePatcher;
    private Dictionary<string, int> OriginalFileSize { get; init; } = new();

    public WingmanPatchService(AssetPatcher aPatcher, FilePatcher patcher, ISourceFileService fileService,
        DirectoryBuildContext context, IModBuilder modBuilder, List<WingmanMod> mods,
        ILogger<ModPatchService<WingmanMod, DirectoryBuildContext>> logger) : base(context, fileService, modBuilder,
        logger)
    {
        _filePatcher = patcher;
        Mods.AddRange(mods);
        _assetPatcher = aPatcher;
    }

    public async Task<ModPatchService<WingmanMod, DirectoryBuildContext>> RunAssetPatches()
    {
        foreach (var mod in Mods)
        foreach (var (targetAssetKey, assetPatchSets) in mod.AssetPatches)
        {
            var targetAsset = targetAssetKey;
            string? targetAssetName = null;
            if (targetAsset.Contains('>'))
            {
                //fancy rewrite incoming
                var assetSplit = targetAsset.Split('>');
                targetAsset = assetSplit.First();
                targetAssetName = assetSplit.Last();
            }

            var srcPath = Path.Join(Context.WorkingDirectory.FullName, targetAsset);
            Logger?.LogDebug("Running asset patches for {asset}", Path.GetFileName(targetAsset));
            _ = await _assetPatcher.RunPatch(srcPath, assetPatchSets, targetAssetName);
        }

        return this;
    }

    public async Task<ModPatchService<WingmanMod, DirectoryBuildContext>> RunAllPatches()
    {
        foreach (var mod in Mods)
        {
            var modifiedFiles = new List<FileInfo>();
            Logger?.LogInformation("Running patches for {mod}", mod.GetLabel(mod.Id ?? "Unknown mod"));
            // _logger?.LogInformation($"Running patches for {mod.Id}");
            foreach (var (targetFile, patchSets) in mod.FilePatches)
            {
                var srcPath = Path.Join(Context.WorkingDirectory.FullName, targetFile);
                OriginalFileSize[srcPath] = (int)new FileInfo(srcPath).Length;
                Logger?.LogDebug("Patching {target}...", Path.GetFileName(targetFile));
                var finalFile = await _filePatcher.RunPatch(srcPath, patchSets);
                modifiedFiles.Add(finalFile);
            }

            Logger?.LogDebug(
                "Modified {modifiedCount} files: {modifiedFiles}",
                modifiedFiles.Count, string.Join(", ", modifiedFiles.Select(static f => f.Name)));
            foreach (var (srcPath, origLength) in OriginalFileSize)
            {
                var fi = new FileInfo(srcPath);
                if (fi.Extension != ".uexp" || fi.Length == origLength) continue;

                Logger.LogWarning("Size change detected in {fileName}: {origLength} -> {newLength}", fi.Name,
                    origLength, fi.Length);
                var uaFile = new FileInfo(fi.FullName.Replace(fi.Extension, ".uasset"));
                if (!uaFile.Exists) continue;

                Logger.LogDebug("Detected matching uasset file, attempting to patch length");
                var lengthBytes = BitConverter.ToString(BitConverter.GetBytes(origLength - 4))
                    .Replace("-", string.Empty);
                var correctedBytes = BitConverter.ToString(BitConverter.GetBytes((int)fi.Length - 4))
                    .Replace("-", string.Empty);
                var lPatch = new PatchSet()
                {
                    Name = "Length auto-correct",
                    Patches = new List<Patch>
                    {
                        new()
                        {
                            Description = "uexp Length",
                            Template = lengthBytes,
                            Value = correctedBytes,
                            Type = "inPlace"
                        }
                    }
                };

                await _filePatcher.RunPatch(uaFile.FullName, new List<PatchSet> { lPatch });
            }
        }

        return this;
    }

    public WingmanPatchService LoadAssetFiles(Func<string, IEnumerable<string>>? extraFileSelector = null)
    {
        var requiredFiles = Mods
            .SelectMany(static em => em.AssetPatches)
            .GroupBy(static fp => fp.Key)
            .Where(static g => g.Any())
            .Select(static g => g.Key)
            .Select(static g => g.Split('>').FirstOrDefault())
            .Where(static g => g != null)
            .Distinct()
            .ToList();

        foreach (var file in requiredFiles)
        {
            Debug.Assert(file != null, "file != null - where clause should have filtered this out");

            var srcFile = FileService.LocateFile(Path.GetFileName(file));
            if (srcFile == null) throw new SourceFileNotFoundException(Path.GetFileName(file));

            BuildContext.AddFile(Path.GetDirectoryName(file) ?? throw new InvalidOperationException(), srcFile);
            if (extraFileSelector == null) continue;

            var extraFiles = extraFileSelector.Invoke(file);
            foreach (var eFile in extraFiles)
            {
                var exFile = FileService.LocateFile(Path.GetFileName(eFile));
                BuildContext.AddFile(Path.GetDirectoryName(eFile) ?? throw new InvalidOperationException(),
                    exFile ?? throw new InvalidOperationException());
            }
        }

        return this;
    }
}

/// <summary>
/// This builder exists only as a very shitty wrapper over the ModPatchService to make it more DI-friendly.
/// Inject a *Builder then use that to create however many services you need.
/// It's shit. I know.
/// </summary>
[PublicAPI]
public sealed class WingmanPatchServiceBuilder
{
    private readonly ISourceFileService _fileService;
    private readonly FilePatcher _filePatcher;
    private readonly AssetPatcher _assetPatcher;
    private readonly DirectoryBuildContextFactory _ctxFactory;
    private readonly IModBuilder _modBuilder;
    private readonly ILogger<ModPatchService<WingmanMod, DirectoryBuildContext>> _tgtLogger;

    public WingmanPatchServiceBuilder(ISourceFileService sourceFileService, FilePatcher filePatcher,
        AssetPatcher assetPatcher, DirectoryBuildContextFactory contextFactory, IModBuilder modBuilder,
        ILogger<ModPatchService<WingmanMod, DirectoryBuildContext>> logger)
    {
        _fileService = sourceFileService;
        _filePatcher = filePatcher;
        _assetPatcher = assetPatcher;
        _ctxFactory = contextFactory;
        _modBuilder = modBuilder;
        _tgtLogger = logger;
    }

    public Task<WingmanPatchService> GetPatchService(IEnumerable<WingmanMod> modCollection, string? ctxName = null)
    {
        var mods = modCollection.ToList();
        var ctx = _ctxFactory.CreateContext(ctxName);

        return Task.FromResult(new WingmanPatchService(_assetPatcher, _filePatcher, _fileService, ctx, _modBuilder,
            mods, _tgtLogger));
    }

    public Task<WingmanPatchEngine> GetPatchEngineService(IEnumerable<WingmanMod> modCollection,
        string? ctxName = null)
    {
        var mods = modCollection.ToList();
        var ctx = _ctxFactory.CreateContext(ctxName);

        return Task.FromResult(new WingmanPatchEngine(mods, ctx, _fileService, _modBuilder, new[]
        {
            new PatchEngineDefinition<WingmanMod, Patch>(new HexPatchEngine(_filePatcher, null),
                static mod =>
                    mod.FilePatches.ToDictionary(static k => k.Key, static v => v.Value.Cast<PatchSet<Patch>>())
            ),
            new PatchEngineDefinition<WingmanMod, Patch>(new AssetPatchEngine(_assetPatcher, null),
                static mod => mod.AssetPatches.ToDictionary(static k => k.Key, static v => v.Value.AsEnumerable()))
        }, null));
    }
}