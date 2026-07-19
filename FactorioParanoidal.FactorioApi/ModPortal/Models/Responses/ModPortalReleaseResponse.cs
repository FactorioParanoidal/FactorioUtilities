using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioApi.ModPortal.Models.Responses;

internal sealed record ModPortalReleaseResponse(
    [property: JsonPropertyName("download_url")]
    string DownloadUrl,
    [property: JsonPropertyName("file_name")]
    string FileName,
    [property: JsonPropertyName("version")]
    string Version,
    [property: JsonPropertyName("released_at")]
    DateTimeOffset? ReleasedAt,
    [property: JsonPropertyName("sha1")] string? Sha1,
    [property: JsonPropertyName("info_json")]
    ModPortalReleaseInfoResponse? InfoJson);