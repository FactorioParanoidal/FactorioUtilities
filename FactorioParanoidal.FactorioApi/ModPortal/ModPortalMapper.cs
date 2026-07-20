using FactorioParanoidal.FactorioApi.Models.Data;
using FactorioParanoidal.FactorioApi.ModPortal.Models.Responses;
using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioApi.ModPortal;

internal static class ModPortalMapper {
    public static ModPage ToModel(this ModPortalResponse response) {
        return new ModPage(
            response.Results.Select(ToModel).ToArray(),
            response.Pagination.Count);
    }

    public static Mod ToModel(this ModPortalModResponse response) {
        return new Mod(
            response.Name,
            response.Owner,
            response.Title,
            response.Summary,
            response.Description,
            response.DownloadsCount,
            response.LatestRelease?.ToModel(),
            response.Releases?.Select(ToModel).ToArray(),
            response.Deprecated,
            response.UpdatedAt,
            response.CreatedAt,
            response.Thumbnail,
            response.SourceUrl,
            response.Homepage,
            response.Tags);
    }

    private static ModRelease ToModel(this ModPortalReleaseResponse response) {
        return new ModRelease(
            response.DownloadUrl,
            response.FileName,
            new FactorioVersion(response.Version),
            response.ReleasedAt,
            response.Sha1,
            response.InfoJson?.ToModel());
    }

    private static ModReleaseInfo ToModel(this ModPortalReleaseInfoResponse response) {
        return new ModReleaseInfo(
            response.Name,
            response.Version is null ? null : new FactorioVersion(response.Version),
            response.FactorioVersion is null ? null : new FactorioVersion(response.FactorioVersion),
            response.Dependencies);
    }
}