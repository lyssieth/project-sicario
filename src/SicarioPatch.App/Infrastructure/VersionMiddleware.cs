using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace SicarioPatch.App.Infrastructure;

[PublicAPI]
public sealed class VersionMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Assembly? EntryAssembly = Assembly.GetEntryAssembly();

    private static readonly string? Version = FileVersionInfo
        .GetVersionInfo(EntryAssembly?.Location ?? throw new InvalidOperationException()).FileVersion;

    public VersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync(Version ?? string.Empty);

        //we're all done, so don't invoke next middleware
    }
}