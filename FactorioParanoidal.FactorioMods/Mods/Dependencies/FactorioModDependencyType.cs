namespace FactorioParanoidal.FactorioMods.Mods.Dependencies;

public enum FactorioModDependencyType {
    /// <summary>
    /// no prefix for a hard requirement for the other mod.
    /// </summary>
    HardRequirement,
    /// <summary>
    /// <c>!</c> for incompatibility
    /// </summary>
    Incompatibility,
    /// <summary>
    /// <c>?</c> for an optional dependency.
    /// </summary>
    Optional,
    /// <summary>
    /// <c>(?)</c> for a hidden optional dependency
    /// </summary>
    HiddenOptional,
    /// <summary>
    /// <c>~</c> for a dependency that does not affect load order
    /// </summary>
    DoesNotAffectLoadOrder
}