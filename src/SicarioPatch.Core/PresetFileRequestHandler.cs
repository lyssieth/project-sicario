using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using SicarioPatch.Engine;

namespace SicarioPatch.Core;

[PublicAPI]
public sealed class PresetFileRequestHandler : IRequestHandler<PresetFileRequest, FileInfo>
{
    private readonly ModParser _parser;
    private readonly IEngineInfoProvider _engineInfo;

    public PresetFileRequestHandler(ModParser parser, IEngineInfoProvider engineInfo)
    {
        _parser = parser;
        _engineInfo = engineInfo;
    }

    public async Task<FileInfo> Handle(PresetFileRequest request, CancellationToken cancellationToken)
    {
        var preset = new WingmanPreset()
        {
            Mods = request.Mods,
            ModParameters = request.TemplateInputs,
            Version = request.Version ?? 1,
            EngineVersion = _engineInfo.GetEngineVersion()
        };
        var tempFile = Path.GetTempFileName();
        var opts = new JsonSerializerOptions(_parser.Options)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(preset, opts);
        await File.WriteAllTextAsync(tempFile, json, Encoding.ASCII, cancellationToken);
        var name = request.PresetName ?? $"preset-{System.DateTime.UtcNow.Ticks}";

        var finalTarget = Path.Combine(Path.GetTempPath(), $"{Path.ChangeExtension(name, ".dtp")}");
        var fi = new FileInfo(finalTarget);
        File.Move(tempFile, fi.FullName);
        return fi;
    }
}