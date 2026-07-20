using FactorioParanoidal.FactorioApi.Models.Data;
using FactorioParanoidal.FactorioApi.ModPortal;
using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioApi.Re146;

public sealed class Re146ModDownloadProvider(HttpClient client) : IModDownloadProvider {
    private static readonly Uri BaseAddress = new("https://mods-storage.re146.dev/");

    public Task<Stream> DownloadAsync(string modName, ModRelease release,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(modName);
        ArgumentNullException.ThrowIfNull(release);
        return DownloadAsync(modName, release.Version, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string modName, FactorioVersion version,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(modName);
        ArgumentNullException.ThrowIfNull(version);

        var uri = new Uri(BaseAddress,
            $"{Uri.EscapeDataString(modName)}/{Uri.EscapeDataString(version.ToString())}.zip");
        var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) {
            var statusCode = response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            throw new ModPortalHttpException(statusCode, uri.ToString(), body);
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}