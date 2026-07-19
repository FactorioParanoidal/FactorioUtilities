using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioApi.ModPortal.Models.Responses;

internal sealed record ModPortalPaginationLinksResponse(
    [property: JsonPropertyName("first")] Uri? First,
    [property: JsonPropertyName("prev")] Uri? Previous,
    [property: JsonPropertyName("next")] Uri? Next,
    [property: JsonPropertyName("last")] Uri? Last);