using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioApi.ModPortal.Models.Requests;

internal sealed record ModPortalModsRequest(
    int Page,
    int PageSize,
    FactorioVersion? Version,
    bool HideDeprecated,
    string Sort,
    string SortOrder,
    IReadOnlyList<string>? Names);