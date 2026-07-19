namespace FactorioParanoidal.FactorioApi.Models.Data;

public sealed record ModPage(
    IReadOnlyList<Mod> Items,
    int Count);