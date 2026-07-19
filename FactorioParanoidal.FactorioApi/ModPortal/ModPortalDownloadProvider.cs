using FactorioParanoidal.FactorioApi.Models.Data;

namespace FactorioParanoidal.FactorioApi.ModPortal;

public sealed class ModPortalDownloadProvider(HttpClient client, ModPortalOptions options) : IModDownloadProvider {
    public async Task<Stream> DownloadAsync(string modName, ModRelease release,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(modName);
        ArgumentNullException.ThrowIfNull(release);
        if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Token))
            throw new InvalidOperationException("Mod Portal username and token are required for downloads.");

        var separator = release.DownloadUrl.Contains('?') ? '&' : '?';
        var uri =
            $"{release.DownloadUrl}{separator}username={Uri.EscapeDataString(options.Username)}&token={Uri.EscapeDataString(options.Token)}";
        var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) {
            var statusCode = response.StatusCode;
            response.Dispose();
            throw new ModPortalHttpException(statusCode, uri);
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}