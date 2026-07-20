using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioApi.Models.Data;

public sealed record ModReleaseInfo(
    string? Name,
    FactorioVersion? Version,
    FactorioVersion? FactorioVersion,
    IReadOnlyList<string>? Dependencies);