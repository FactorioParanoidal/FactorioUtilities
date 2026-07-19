namespace FactorioParanoidal.FactorioApi.Models.Data;

public sealed record ModRelease(
    string DownloadUrl,
    string FileName,
    string Version,
    DateTimeOffset? ReleasedAt,
    string? Sha1,
    ModReleaseInfo? InfoJson);