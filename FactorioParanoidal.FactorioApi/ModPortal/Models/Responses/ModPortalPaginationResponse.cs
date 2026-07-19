using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioApi.ModPortal.Models.Responses;

internal sealed record ModPortalPaginationResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("page_count")]
    int PageCount,
    [property: JsonPropertyName("page_size")]
    int PageSize,
    [property: JsonPropertyName("links")] ModPortalPaginationLinksResponse? Links);