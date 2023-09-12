﻿using System;
using System.IO;
using System.IO.Compression;
using JetBrains.Annotations;

namespace SicarioPatch.Core;

[PublicAPI]
public static class ArchiveHelpers
{
    public static FileInfo ToZipFile(this DirectoryInfo dirInfo, DirectoryInfo? targetPath = null)
    {
        var path = Path.Combine(targetPath?.FullName ?? Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        ZipFile.CreateFromDirectory(dirInfo.FullName, path);
        return new FileInfo(path);
    }
}