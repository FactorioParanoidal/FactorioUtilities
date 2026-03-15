using System.IO.Compression;
using System.Text.Json;

namespace FactorioParanoidal.FactorioMods.Mods;

public class ZipFactorioMod : IFactorioMod {
    private readonly string _rootInZip;
    private readonly string _zipPath;

    public ZipFactorioMod(FactorioModInfo info, string zipPath, string rootInZip) {
        Info = info;
        _zipPath = zipPath;
        _rootInZip = rootInZip;
    }

    public FactorioModInfo Info { get; }

    public bool FileExists(string subPath) {
        using var archive = ZipFile.OpenRead(_zipPath);
        var entryPath = Path.Combine(_rootInZip, subPath).Replace('\\', '/');
        return archive.GetEntry(entryPath) != null;
    }

    public async Task<string> ReadFileTextAsync(string subPath, CancellationToken cancellationToken = default) {
        await using var archive = ZipFile.OpenRead(_zipPath);
        var entryPath = Path.Combine(_rootInZip, subPath).Replace('\\', '/');
        var entry = archive.GetEntry(entryPath);
        if (entry == null) {
            throw new FileNotFoundException($"File {subPath} not found in mod ZIP {_zipPath}");
        }

        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public static async Task<ZipFactorioMod> LoadFromFile(string zipPath) {
        try {
            zipPath = Path.GetFullPath(zipPath);
            await using var archive = await ZipFile.OpenReadAsync(zipPath);

            // Factorio mods in ZIPs usually have a single root directory: <name>_<version>/
            // We need to find info.json to determine the actual root and metadata.
            var infoEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith(IFactorioMod.InfoJsonPath, StringComparison.OrdinalIgnoreCase));

            if (infoEntry == null) {
                throw new FactorioModLoadException($"info.json not found in mod ZIP {zipPath}", null);
            }

            var rootInZip = infoEntry.FullName[..^IFactorioMod.InfoJsonPath.Length];

            await using var stream = await infoEntry.OpenAsync();
            var modInfo = await JsonSerializer.DeserializeAsync<FactorioModInfo>(stream);

            if (modInfo == null) {
                throw new FactorioModLoadException($"Failed to deserialize info.json in mod ZIP {zipPath}", null);
            }

            return new ZipFactorioMod(modInfo, zipPath, rootInZip);
        }
        catch (Exception e) when (e is not FactorioModLoadException) {
            throw new FactorioModLoadException(
                $"Factorio mod loading from {zipPath} failed. See inner exception for details", e);
        }
    }
}