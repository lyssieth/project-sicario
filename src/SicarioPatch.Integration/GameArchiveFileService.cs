using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using LiteDB;
using ModEngine.Build;
using UnPak.Core;

namespace SicarioPatch.Integration;

[PublicAPI]
public sealed class GameArchiveFileService : ISourceFileService, IDisposable
{
    private readonly PakFileProvider _pakFileProvider;
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<UnpackedFile> _files;
    private readonly IEnumerable<IGameSource> _gameSources;
    private DirectoryInfo WorkingDirectory { get; init; }

    public FileInfo? LocateFile(string fileName)
    {
        var srcHash = GetCurrentSourceHash();
        var localFile = _files.FindOne(f => f.AssetPath == fileName);
        if (localFile != null && !string.IsNullOrWhiteSpace(localFile.SourceIndexHash) &&
            localFile.SourceIndexHash == srcHash && !string.IsNullOrWhiteSpace(localFile.OutputPath))
        {
            var localFilePath = Path.Combine(WorkingDirectory.FullName,
                localFile.OutputPath);
            if (File.Exists(localFilePath))
            {
                return new FileInfo(localFilePath);
            }
            else
            {
                // ignored, the upsert below should fix this anyway
                // _files.DeleteMany(f => f.AssetPath == fileName);
            }
        }

        var unpacked = UnpackFile(fileName, out srcHash);
        if (unpacked is not { Exists: true }) return null;

        _files.Upsert(new UnpackedFile
        {
            AssetPath = fileName,
            OutputPath = Path.GetRelativePath(WorkingDirectory.FullName, unpacked.FullName),
            SourceIndexHash = srcHash
        });
        _files.EnsureIndex(static uf => uf.AssetPath);
        return unpacked;
    }

    private FileInfo? UnpackFile(string filePath, out string? hash)
    {
        try
        {
            var gamePak = GetGamePakPath();
            if (!string.IsNullOrWhiteSpace(gamePak?.FullName))
            {
                var fs = new FileStream(gamePak.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var reader = _pakFileProvider.GetReader(fs);
                var pakFile = reader.ReadFile();
                var outputFile = pakFile.FirstOrDefault(r =>
                    r.FileName.ToLower().TrimStart('/') == filePath.ToLower().TrimStart('/')) ?? pakFile.FirstOrDefault(
                    r =>
                        Path.GetFileName(r.FileName).ToLower().TrimStart('/') == filePath.ToLower().TrimStart('/'));
                var unpacked = outputFile?.Unpack(fs, WorkingDirectory);
                hash = pakFile.FileFooter.IndexHash;
                return unpacked;
            }
        }
        catch (Exception)
        {
            //ignored
        }

        hash = null;
        return null;
    }

    private FileInfo? GetGamePakPath()
    {
        var pakPath = _gameSources.Select(static gs => gs.GetGamePakPath())
            .FirstOrDefault(static gp => !string.IsNullOrWhiteSpace(gp));
        if (pakPath != null && File.Exists(pakPath)) return new FileInfo(pakPath);

        return null;
    }

    private string? GetCurrentSourceHash()
    {
        var gamePak = GetGamePakPath();
        if (string.IsNullOrWhiteSpace(gamePak?.FullName)) return null;

        var reader = _pakFileProvider.GetReader(gamePak);
        var footer = reader.ReadFooter();
        return footer?.IndexHash;
    }

    public GameArchiveFileService(PakFileProvider pakFileProvider, IEnumerable<IGameSource> gameSources,
        string? workingPath = null)
    {
        _pakFileProvider = pakFileProvider;
        WorkingDirectory = new DirectoryInfo(workingPath ??
                                             Path.Join(_gameSources?.GetGamePath() ?? Environment.CurrentDirectory,
                                                 "ProjectWingman-Unpacked"));
        var dbPath = Path.Join(WorkingDirectory.FullName, "srcFiles.db");
        _db = new LiteDatabase(dbPath);
        _files = _db.GetCollection<UnpackedFile>("unpacked");
        _files.EnsureIndex(static uf => uf.AssetPath);
        _gameSources = gameSources;
    }

    public void Dispose()
    {
        _db.Dispose();
        // GC.SuppressFinalize(this);
    }
}