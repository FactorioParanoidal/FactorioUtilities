namespace FactorioParanoidal.FactorioApi.Models.Data;

public sealed record Mod(
    string Name,
    string? Owner,
    string? Title,
    string? Summary,
    string? Description,
    int DownloadsCount,
    ModRelease? LatestRelease,
    IReadOnlyList<ModRelease>? Releases,
    bool Deprecated,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? CreatedAt,
    string? Thumbnail,
    string? SourceUrl,
    string? Homepage,
    IReadOnlyList<string>? Tags);