namespace FactorioParanoidal.FactorioMods.Mods.Dependencies;

public class FactorioModDependencyEqualityVersion {
    public FactorioModDependencyEquality Equality { get; set; } = FactorioModDependencyEquality.GreaterOrEqual;
    public required Version Version { get; set; }
}