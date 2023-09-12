using System.Collections.Generic;
using MediatR;
using ModEngine.Merge;
using SicarioPatch.Core;

namespace SicarioPatch.Loader;

public sealed class MergeComponentRequest : IRequest<IEnumerable<MergeComponent<WingmanMod>>>
{
    internal List<string>? SearchPaths { get; init; }
}