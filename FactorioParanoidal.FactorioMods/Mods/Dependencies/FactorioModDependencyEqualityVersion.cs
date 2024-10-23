namespace FactorioParanoidal.FactorioMods.Mods.Dependencies;

public class FactorioModDependencyEqualityVersion {
    public FactorioModDependencyEquality Equality { get; set; } = FactorioModDependencyEquality.GreaterOrEqual;
    public required Version Version { get; set; }
    
    public bool IsSatisfied(Version version) {
        return Equality switch {
            FactorioModDependencyEquality.Less => version < Version,
            FactorioModDependencyEquality.LessOrEqual => version <= Version,
            FactorioModDependencyEquality.Equal => version == Version,
            FactorioModDependencyEquality.Greater => version > Version,
            FactorioModDependencyEquality.GreaterOrEqual => version >= Version,
            _ => throw new ArgumentOutOfRangeException(nameof(FactorioModDependencyEquality))
        };
    }
}