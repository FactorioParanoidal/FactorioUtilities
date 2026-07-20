using FactorioParanoidal.FactorioApi.Models.Data;
using FactorioParanoidal.FactorioApi.Models.Requests;
using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioApi.ModPortal;

public sealed class ModPortal(
    IModInfoProvider infoProvider,
    IModDownloadProvider downloadProvider) : IFactorioApi {
    public Task<ModPage> GetModsAsync(ModQuery? query = null, CancellationToken cancellationToken = default) =>
        infoProvider.GetModsAsync(query, cancellationToken);

    public Task<Mod> GetModAsync(string name, bool full = false, CancellationToken cancellationToken = default) =>
        infoProvider.GetModAsync(name, full, cancellationToken);

    public Task<Stream> DownloadAsync(string modName, FactorioVersion version,
        CancellationToken cancellationToken = default) =>
        downloadProvider.DownloadAsync(modName, version, cancellationToken);

    public Task<Stream> DownloadAsync(string modName, ModRelease release,
        CancellationToken cancellationToken = default) =>
        downloadProvider.DownloadAsync(modName, release, cancellationToken);
}