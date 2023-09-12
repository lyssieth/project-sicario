using System;
using System.IO;
using BuildEngine;
using HexPatch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModEngine.Build;
using ModEngine.Merge;
using SicarioPatch.Core;
using SicarioPatch.Integration;
using SicarioPatch.Loader.Providers;
using SicarioPatch.Templating;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;
using UnPak.Core;
using UnPak.Core.Crypto;

namespace SicarioPatch.Loader;

internal static class Startup
{
    private static IServiceCollection GetServices()
    {
        var services = new ServiceCollection()
            .AddSingleton<IGameSource>(static _ =>
            {
                if (OperatingSystem.IsLinux())
                    return new LinuxGameFinder();
                if (OperatingSystem.IsWindows()) return new WindowsGameFinder();

                throw new NotSupportedException("Unsupported operating system");
            })
            .AddSingleton<IGameSource, ConfigurationGameSource>()
            .AddSingleton<SkinSlotLoader>()
            .AddSingleton<GameArchiveFileService>(static p =>
            {
                var unpackRoot = Path.Combine(Path.GetTempPath(), "ProjectWingman-Unpacked");
                unpackRoot.EnsureDirectoryExists();
                return new GameArchiveFileService(p.GetRequiredService<PakFileProvider>(), p.GetServices<IGameSource>(),
                    unpackRoot);
            })
            .AddSingleton<MergeLoader>()
            .AddCoreServices()
            .AddMergeComponents()
            .AddUnPak()
            .AddMediatR(static mc =>
            {
                mc.Lifetime = ServiceLifetime.Scoped;
                mc.RegisterServicesFromAssemblyContaining(typeof(Startup));
                mc.RegisterServicesFromAssemblyContaining<PatchRequest>();
            })
            .AddBehaviours()
            .AddTemplating()
            .AddConfiguration()
            .AddLogging();
        return services;
    }

    internal static CommandApp GetApp()
    {
        var level = GetLogLevel();
        var app = new CommandApp(new DependencyInjectionRegistrar(GetServices()));
        app.SetDefaultCommand<BuildCommand>();
        app.Configure(c =>
        {
            if (level < LogLevel.Information) c.PropagateExceptions();
            // c.PropagateExceptions();
            c.AddCommand<BuildCommand>("build");
            c.AddCommand<PresetPackCommand>("preset-pack");
        });
        return app;
    }

    private static IServiceCollection AddUnPak(this IServiceCollection services)
    {
        return services.AddSingleton<IPakFormat, PakVersion3Format>()
            .AddSingleton<IPakFormat, PakVersion8Format>()
            .AddSingleton<IFooterLayout, DefaultFooterLayout>()
            .AddSingleton<IFooterLayout, PaddedFooterLayout>()
            .AddSingleton<IHashProvider, NativeHashProvider>()
            .AddSingleton<PakFileProvider>();
    }

    private static IServiceCollection AddConfiguration(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        return services.AddSingleton(config).AddSingleton<IConfiguration>(config);
    }

    private static IServiceCollection AddLogging(this IServiceCollection services, LogLevel? logLevel = null)
    {
        return services.AddLogging(logging =>
        {
            var level = logLevel ?? GetLogLevel();
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddInlineSpectreConsole(c => { c.LogLevel = level; });
            // AddFileLogging(logging, level);
        });
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<WingmanPatchServiceBuilder>()
            .AddSingleton<ISourceFileService>(static p => p.GetRequiredService<GameArchiveFileService>())
            .AddSingleton<ModParser>()
            .AddSingleton<FilePatcher>()
            .AddSingleton<DirectoryBuildContextFactory>()
            .AddSingleton<IAppInfoProvider, AppInfoProvider>()
            .AddSingleton<AppInfoProvider>()
            .AddSingleton<IModBuilder, UnPakBuilder>()
            .AddAssetServices();
    }

    private static IServiceCollection AddMergeComponents(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMergeProvider<WingmanMod>, EmbeddedResourceProvider>()
            .AddSingleton<IMergeProvider<WingmanMod>, LoosePresetProvider>()
            .AddSingleton<IMergeProvider<WingmanMod>, SkinMergeProvider>();
    }

    private static LogLevel GetLogLevel()
    {
        var envVar = Environment.GetEnvironmentVariable("SICARIO_DEBUG");
        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "sicario-debug.txt"))) envVar = "trace";
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "sicario-debug.txt"))) envVar = "trace";
        return string.IsNullOrWhiteSpace(envVar)
            ? LogLevel.Information
            : envVar.ToLower() == "trace"
                ? LogLevel.Trace
                : LogLevel.Debug;
    }
}