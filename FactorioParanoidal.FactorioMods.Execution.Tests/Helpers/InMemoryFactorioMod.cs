using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioMods.Execution.Tests.Helpers;

public class InMemoryFactorioMod : IFactorioMod {
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryFactorioMod(FactorioModInfo info) {
        Info = info;
    }

    public FactorioModInfo Info { get; }

    public bool FileExists(string subPath) {
        return _files.ContainsKey(subPath);
    }

    public Task<string> ReadFileTextAsync(string subPath, CancellationToken cancellationToken = default) {
        if (_files.TryGetValue(subPath, out var content)) {
            return Task.FromResult(content);
        }

        throw new FileNotFoundException($"File not found in memory mod: {subPath}");
    }

    public void AddFile(string subPath, string content) {
        _files[subPath] = content;
    }
}