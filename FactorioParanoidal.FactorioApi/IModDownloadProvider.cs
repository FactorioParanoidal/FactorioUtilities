using FactorioParanoidal.FactorioApi.Models.Data;
using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioApi;

public interface IModDownloadProvider {
    Task<Stream> DownloadAsync(string modName, FactorioVersion version,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string modName, ModRelease release, CancellationToken cancellationToken = default);
}