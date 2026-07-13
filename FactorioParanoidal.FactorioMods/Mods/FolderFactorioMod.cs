using System.Text.Json;

namespace FactorioParanoidal.FactorioMods.Mods;

public class FolderFactorioMod : IFactorioMod {
    // Lazily built set of all subpaths (relative, forward-slash, lower-case) to avoid repeated File.Exists calls
    // during data-stage require resolution. Built on first FileExists call.
    private HashSet<string>? _fileIndex;

    public FolderFactorioMod(FactorioModInfo info, string directory) {
        Info = info;
        Directory = directory;
    }

    public string Directory { get; set; }

    public FactorioModInfo Info { get; }

    public bool FileExists(string subPath) {
        var index = _fileIndex ??= BuildIndex();
        return index.Contains(Normalize(subPath));
    }

    public Task<string> ReadFileTextAsync(string subPath, CancellationToken cancellationToken = default) {
        return File.ReadAllTextAsync(Path.Combine(Directory, subPath), cancellationToken);
    }

    private HashSet<string> BuildIndex() {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, "*", SearchOption.AllDirectories)) {
            var rel = Path.GetRelativePath(Directory, file).Replace('\\', '/');
            set.Add(rel);
        }

        return set;
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    public static async Task<FolderFactorioMod> LoadFromDirectory(string modDirectory) {
        try {
            modDirectory = Path.GetFullPath(modDirectory);
            var infoJson = Path.Combine(modDirectory, IFactorioMod.InfoJsonPath);
            await using var fileStream = File.OpenRead(infoJson);
            var modInfo = await JsonSerializer.DeserializeAsync<FactorioModInfo>(fileStream);
            return new FolderFactorioMod(modInfo!, modDirectory);
        }
        catch (Exception e) {
            throw new FactorioModLoadException(
                $"Factorio mod loading from {modDirectory} failed. See inner exception for details", e);
        }
    }
}