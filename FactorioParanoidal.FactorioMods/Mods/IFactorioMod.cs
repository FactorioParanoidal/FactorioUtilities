namespace FactorioParanoidal.FactorioMods.Mods;

public interface IFactorioMod {
    public const string InfoJsonPath = "info.json";
    public FactorioModInfo Info { get; }

    bool FileExists(string subPath);
    Task<string> ReadFileTextAsync(string subPath, CancellationToken cancellationToken = default);
}