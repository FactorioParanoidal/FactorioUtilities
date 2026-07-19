using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioApi.ModPortal.Models.Responses;

internal sealed record ModPortalModResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("owner")] string? Owner,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("summary")]
    string? Summary,
    [property: JsonPropertyName("description")]
    string? Description,
    [property: JsonPropertyName("downloads_count")]
    int DownloadsCount,
    [property: JsonPropertyName("latest_release")]
    ModPortalReleaseResponse? LatestRelease,
    [property: JsonPropertyName("releases")]
    IReadOnlyList<ModPortalReleaseResponse>? Releases,
    [property: JsonPropertyName("deprecated")]
    bool Deprecated,
    [property: JsonPropertyName("updated_at")]
    DateTimeOffset? UpdatedAt,
    [property: JsonPropertyName("created_at")]
    DateTimeOffset? CreatedAt,
    [property: JsonPropertyName("thumbnail")]
    string? Thumbnail,
    [property: JsonPropertyName("source_url")]
    string? SourceUrl,
    [property: JsonPropertyName("homepage")]
    string? Homepage,
    [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags);