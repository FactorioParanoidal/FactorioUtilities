using System.Formats.Tar;
using System.Text.Json;
using SharpCompress.Compressors.Xz;
using SharpCompress.IO;

namespace FactorioParanoidal.FactorioMods.Mods;

/// <summary>
///     Downloads and caches the built-in Factorio game-data mods (core, base, elevated-rails, quality, space-age)
///     from the public <b>headless</b> Factorio packages on factorio.com.
///     The headless package ships the full data-stage Lua and prototype definitions but no graphics/audio binaries,
///     which is exactly what the data-stage executor needs (prototypes reference assets as plain path strings).
///     No authentication is required to download headless packages.
/// </summary>
public static class FactorioGameData {
    public const string LatestReleasesUrl = "https://factorio.com/api/latest-releases";

    /// <summary>Layout inside the headless archive: <c>factorio/data/&lt;mod&gt;/...</c>.</summary>
    private const string DataPrefix = "factorio/data/";

    private const string DownloadCompleteMarker = ".download-complete";

    /// <summary>Built-in mods bundled inside the headless package under <c>factorio/data/</c>.</summary>
    public static readonly IReadOnlyList<string> BuiltinModNames =
        ["core", "base", "elevated-rails", "quality", "space-age"];

    private static readonly HttpClient _httpClient = new();

    /// <summary>Resolves the latest headless version published on factorio.com.</summary>
    public static async Task<Version> GetLatestHeadlessVersionAsync(
        bool stable = true, CancellationToken cancellationToken = default) {
        await using var stream = await _httpClient.GetStreamAsync(LatestReleasesUrl, cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var channel = stable ? "stable" : "experimental";
        var versionString = document.RootElement
            .GetProperty(channel)
            .GetProperty("headless")
            .GetString() ?? throw new InvalidOperationException(
            $"Could not read headless version from {LatestReleasesUrl}");
        return new Version(versionString);
    }

    /// <summary>
    ///     Ensures the headless game data for <paramref name="version" /> is downloaded and extracted into the cache,
    ///     returning the directory that contains the built-in mod folders (core, base, ...).
    /// </summary>
    public static async Task<string> EnsureDownloadedAsync(
        Version version, string? cacheRoot = null, CancellationToken cancellationToken = default) {
        cacheRoot ??= ".factorio-game-data";
        var versionString = version.ToString(3);
        var dataPath = Path.Combine(cacheRoot, versionString);
        var marker = Path.Combine(dataPath, DownloadCompleteMarker);

        if (File.Exists(marker)) {
            return dataPath;
        }

        // Clean up any partial/incomplete extraction from a previous interrupted run.
        if (Directory.Exists(dataPath)) {
            Directory.Delete(dataPath, true);
        }

        Directory.CreateDirectory(dataPath);

        var downloadUrl = $"https://factorio.com/get-download/{versionString}/headless/linux64";

        using var response = await _httpClient.GetAsync(
            downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var bufferedStream = SharpCompressStream.Create(responseStream);
        await using var xzStream = new XZStream(bufferedStream);
        await using var tarReader = new TarReader(xzStream, true);

        while (await tarReader.GetNextEntryAsync(cancellationToken: cancellationToken) is { } entry) {
            if (entry.EntryType is TarEntryType.GlobalExtendedAttributes or TarEntryType.Directory) {
                continue;
            }

            var name = entry.Name.Replace('\\', '/').TrimStart('.', '/');
            if (!name.StartsWith(DataPrefix, StringComparison.Ordinal)) {
                continue;
            }

            var relativePath = name[DataPrefix.Length..];
            var filePath = Path.GetFullPath(Path.Combine(dataPath, relativePath));

            // Defense against path traversal (zip-slip variant): never let an entry escape the cache directory.
            if (!filePath.StartsWith(Path.GetFullPath(dataPath) + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal)) {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await entry.ExtractToFileAsync(filePath, true, cancellationToken);
        }

        PatchCoreInfoVersion(dataPath, version);

        await File.WriteAllTextAsync(marker, versionString, cancellationToken);
        return dataPath;
    }

    /// <summary>
    ///     Downloads (if needed) and loads the built-in game-data mods as <see cref="FolderFactorioMod" /> instances.
    /// </summary>
    public static async Task<IReadOnlyList<FolderFactorioMod>> LoadBuiltinModsAsync(
        Version version, string? cacheRoot = null, CancellationToken cancellationToken = default) {
        var dataPath = await EnsureDownloadedAsync(version, cacheRoot, cancellationToken);

        var mods = new List<FolderFactorioMod>();
        foreach (var name in BuiltinModNames) {
            var modPath = Path.Combine(dataPath, name);
            if (File.Exists(Path.Combine(modPath, IFactorioMod.InfoJsonPath))) {
                mods.Add(await FolderFactorioMod.LoadFromDirectory(modPath));
            }
        }

        return mods;
    }

    // core/info.json ships without a "version" field, but FactorioModInfo.Version is required for deserialization
    // and load ordering. Inject the package version so core can be loaded like any other mod.
    private static void PatchCoreInfoVersion(string dataPath, Version version) {
        var coreInfoPath = Path.Combine(dataPath, "core", IFactorioMod.InfoJsonPath);
        if (!File.Exists(coreInfoPath)) {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(coreInfoPath));
        if (document.RootElement.TryGetProperty("version", out _)) {
            return;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true })) {
            writer.WriteStartObject();
            writer.WriteString("version", version.ToString(3));
            foreach (var property in document.RootElement.EnumerateObject()) {
                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        File.WriteAllBytes(coreInfoPath, stream.ToArray());
    }
}