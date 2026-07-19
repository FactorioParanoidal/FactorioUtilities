using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioApi.ModPortal.Models.Responses;

internal sealed record ModPortalReleaseInfoResponse(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("version")]
    string? Version,
    [property: JsonPropertyName("factorio_version")]
    string? FactorioVersion,
    [property: JsonPropertyName("dependencies")]
    IReadOnlyList<string>? Dependencies);