using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioApi.ModPortal.Models.Responses;

internal sealed record ModPortalResponse(
    [property: JsonPropertyName("pagination")]
    ModPortalPaginationResponse Pagination,
    [property: JsonPropertyName("results")]
    IReadOnlyList<ModPortalModResponse> Results);