using System.Collections.Immutable;

namespace BUTR.ModListServer.Models
{
    public sealed record ModList(string Version, ImmutableArray<ModListModule> Modules);
}