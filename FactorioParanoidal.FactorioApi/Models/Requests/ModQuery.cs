using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioApi.Models.Requests;

public sealed record ModQuery(
    int Page = 1,
    int PageSize = 25,
    FactorioVersion? FactorioVersion = null,
    bool HideDeprecated = true,
    string Sort = "name",
    string SortOrder = "desc",
    IReadOnlyList<string>? Names = null);