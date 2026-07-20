using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioApi.Models.Data;

public sealed record ModRelease(
    string DownloadUrl,
    string FileName,
    FactorioVersion Version,
    DateTimeOffset? ReleasedAt,
    string? Sha1,
    ModReleaseInfo? InfoJson);