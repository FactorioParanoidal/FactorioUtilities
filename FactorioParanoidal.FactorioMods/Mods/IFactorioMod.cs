namespace FactorioParanoidal.FactorioMods.Mods;

public interface IFactorioMod {
    public const string InfoJsonPath = "info.json";
    public FactorioModInfo Info { get; }
}