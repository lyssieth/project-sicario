﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModEngine.Build.Diagnostics;
using ModEngine.Merge;
using SicarioPatch.Core;
using SicarioPatch.Engine;
using SicarioPatch.Integration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SicarioPatch.Loader;

[PublicAPI]
public sealed class BuildCommand : AsyncCommand<BuildCommand.Settings>
{
    private readonly IAnsiConsole _console;
    private readonly SkinSlotLoader _slotLoader;
    private readonly IMediator _mediator;
    private readonly MergeLoader _mergeLoader;
    private readonly IConfiguration _config;
    private readonly IGameSource _gameSource;
    private readonly ILogger<BuildCommand> _logger;
    private readonly ModParser _parser;
    private readonly IEngineInfoProvider _engineInfo;

    [PublicAPI]
    public class Settings : CommandSettings
    {
        [CommandOption("-r|--run")]
        [Description("Attempts to launch the game after completing the merge build.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FlagValue<bool> RunAfterBuild { get; set; }

        [CommandOption("--installPath")]
        [Description("Sets the game install path. Overrides the automatic detection.")]
        public string? InstallPath { get; set; }

        [CommandArgument(0, "[presetPaths]")] public string[]? PresetPaths { get; init; }

        [CommandOption("--no-clean")]
        [Description("Do not remove existing files in the merge output directory.")]
        public FlagValue<bool> SkipTargetClean { get; init; }

        [CommandOption("--outputPath")]
        [Description("Set the path to write the merged mod to.")]
        public string? OutputPath { get; set; }

        [CommandOption("-q|--quiet")]
        [Description("Reduce the amount of information written to the console.")]
        public FlagValue<bool> Quiet { get; set; }

        [CommandOption("--non-interactive")]
        [Description("Ensures that there are no prompts or confirmations while building.")]
        public bool NonInteractive { get; set; }

        [CommandOption("--report")]
        [Description(
            "Writes a report file with the given name with details of the merged mod build. Format subject to change.")]
        public string? ReportFile { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }

    public BuildCommand(IAnsiConsole console, SkinSlotLoader slotLoader, IMediator mediator,
        MergeLoader mergeLoader, IConfiguration config,
        IGameSource gameSource, ILogger<BuildCommand> logger, ModParser parser,
        IEngineInfoProvider engineInfo)
    {
        _console = console;
        _slotLoader = slotLoader;
        _mediator = mediator;
        _mergeLoader = mergeLoader;
        _config = config;
        _gameSource = gameSource;
        _logger = logger;
        _parser = parser;
        _engineInfo = engineInfo;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.InstallPath))
        {
            var install = _gameSource.GetGamePath();
            install ??= new LocalGameFinder().GetGamePath() ??
                        new LocalGameFinder(Environment.CurrentDirectory).GetGamePath();
            if (install == null)
            {
                LogConsole("[bold red]Error![/] [orange3]Could not locate game install folder![/]");
                return 412;
            }

            settings.InstallPath = install;
        }

        if (!Directory.Exists(settings.InstallPath))
        {
            LogConsole("[red][bold]Install not found![/] The game install directory doesn't exist.[/]");
            return 404;
        }

        var paksRoot = Path.Join(settings.InstallPath, "ProjectWingman", "Content", "Paks");

        _config["GamePath"] = settings.InstallPath;
        _config["GamePakPath"] = Path.Join(paksRoot, "ProjectWingman-WindowsNoEditor.pak");

        _logger.LogInformation("Running engine version {engineVersion}", _engineInfo.GetEngineVersion() ?? "unknown");

        var presetSearchPaths = new List<string>(settings.PresetPaths ?? Array.Empty<string>())
        {
            Path.Join(settings.InstallPath, "ProjectWingman", "Content", "Presets"),
            Path.Join(paksRoot, "~mods"),
            Path.Join(paksRoot, "~presets")
        };

        _logger.LogDebug("Searching {presetSearchPathsCount} paths for merge components", presetSearchPaths.Count);

        var mergeReq = new MergeComponentRequest
        {
            SearchPaths = presetSearchPaths
        };
        var components = (await _mediator.Send(mergeReq)).ToList();
        if (!settings.Quiet.Is(true))
            foreach (var mergeComponent in components.Where(static mergeComponent =>
                         !string.IsNullOrWhiteSpace(mergeComponent.Message)))
                LogConsole($"{mergeComponent.Message}");


        //final merge

        var inputParameterList = components.GetParameters();

        LogConsole($"Final mod will be built with [dodgerblue2]{inputParameterList.Keys.Count}[/] parameters");

        var modList = components.GetMods();

        LogConsole($"[bold darkblue]Queuing mod build with {modList.Count} mods[/]");

        var targetPath = string.IsNullOrWhiteSpace(settings.OutputPath)
            ? Path.Join(paksRoot, "~sicario")
            : settings.OutputPath;


        var req = new PatchRequest(modList)
        {
            Id = string.Empty,
            PackResult = true,
            TemplateInputs = inputParameterList,
            Name = "SicarioMerge",
            UserName = $"loader:{Environment.MachineName}"
        };
        try
        {
            var resp = await _mediator.Send(req);
            if (string.IsNullOrWhiteSpace(settings.OutputPath))
            {
                LogConsole(
                    "[green][bold]Success![/] Your merged mod has been built and is now being installed to the game folder[/]");
                var isVortexManaged = CheckForDeploymentManifest(paksRoot);
                if (isVortexManaged)
                {
                    LogConsole("[orange3][bold]Warning![/] Your mods folder appears to be Vortex-managed![/]");
                    LogConsole(
                        "We recommend using Vortex's PSM integration to manage your merged mod automatically.");
                    var toContinue = settings.NonInteractive || ((!settings.Quiet.IsSet || !settings.Quiet.Value) &&
                                                                 _console.Prompt(
                                                                     new ConfirmationPrompt(
                                                                         "Do you want to continue with this build anyway?")));
                    if (!toContinue) return 204;
                }

                BuildTargetPath(targetPath);

                resp.MoveTo(Path.Join(targetPath, resp.Name), true);
                LogConsole("[dodgerblue2]Your merged mod is installed and you can start the game.[/]");
            }
            else
            {
                LogConsole(
                    "[green3_1][bold]Success![/] Your merged mod has been built and is now being installed to the specified output folder[/]");
                BuildTargetPath(targetPath);

                resp.MoveTo(Path.Join(targetPath, resp.Name), true);
                LogConsole($"[dodgerblue2]Your merged mod is built in the [grey]'{targetPath}'[/] directory.[/]");
            }
        }
        catch (SourceFileNotFoundException sEx)
        {
            _logger.LogError(sEx, "Error while building mod, source file not found.");
            return 412;
        }
        catch (AssetInstructionException aEx)
        {
            _logger.LogError(aEx,
                "Error while running asset instructions, this is likely a bad patch or incorrect duplication.");
            return 422;
        }
        catch (Exception e)
        {
            _logger.LogError("An unhandled error was encountered while building the mod file.");
            // _logger.LogDebug(e.Message);
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogError(e,
                    "Error while building {modListCount} mods with {inputParameterListCount} parameters.",
                    modList.Count,
                    inputParameterList.Count);
#if DEBUG
            Console.WriteLine(e.StackTrace);
#endif
            return 400;
        }

        if (!string.IsNullOrWhiteSpace(settings.ReportFile))
            try
            {
                //build report
                // this could probably be a mediator publish but lets leave it for now
                if (!Path.IsPathRooted(settings.ReportFile))
                    settings.ReportFile = Path.Join(targetPath, settings.ReportFile);

                var writer = new JsonReportWriter<WingmanMod>(_parser.RelaxedOptions);
                var reportFile = await writer.WriteReport(components, inputParameterList, settings.ReportFile);
                if (reportFile != null && File.Exists(reportFile.AbsoluteUri))
                    LogConsole($"Wrote merge report to '{reportFile.AbsoluteUri}'.");
            }
            catch (Exception e)
            {
                // report is considered non-essential, so ignoring errors here!
                _logger.LogWarning(e, "Error encountered while writing report file!");
            }

        if (settings.RunAfterBuild is { IsSet: true, Value: true })
        {
            var launcher = new GameLauncher(settings.InstallPath);
            launcher.RunGame();
            // because we're using an ancient version of ExecEngine
            // this will actually wait for the game to exit.
            // not ideal, but not a deal-breaker imo
        }

        if (settings.NonInteractive) return 0;

        // Console.WriteLine();
        Console.WriteLine();
        LogConsole("The PSM merge build is now complete!".PadLeft(6));
        LogConsole("[bold]Press [green3_1]<ENTER>[/] to close this window![/]".PadLeft(6));
        Console.ReadLine();

        return 0;

        void BuildTargetPath(string s)
        {
            if (!Directory.Exists(s)) Directory.CreateDirectory(s);

            if (!Directory.GetFiles(s).Any() ||
                settings.SkipTargetClean is { IsSet: true, Value: true }) return;

            foreach (var file in Directory.GetFiles(s))
                File.Delete(file);
        }

        void LogConsole(string message)
        {
            if (settings.Quiet is { IsSet: true, Value: true }) return;

            _console.MarkupLine(message);
        }
    }

    private static bool CheckForDeploymentManifest(string paksPath)
    {
        var files = new DirectoryInfo(paksPath).EnumerateFiles("vortex.deployment.json",
            SearchOption.AllDirectories);
        return files.Any(static f => f.Length > 0);
    }
}