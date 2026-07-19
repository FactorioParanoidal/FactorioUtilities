using FactorioParanoidal.FactorioApi.Models.Data;

namespace FactorioParanoidal.FactorioApi;

public interface IModDownloadProvider {
    Task<Stream> DownloadAsync(string modName, Version version, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string modName, ModRelease release, CancellationToken cancellationToken = default);
}