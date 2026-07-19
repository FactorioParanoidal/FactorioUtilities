namespace FactorioParanoidal.FactorioApi.Models.Data;

public sealed record ModReleaseInfo(
    string? Name,
    string? Version,
    string? FactorioVersion,
    IReadOnlyList<string>? Dependencies);