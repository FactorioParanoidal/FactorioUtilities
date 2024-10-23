using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Mods.Dependencies;

[JsonConverter(typeof(FactorioModDependencyConverter))]
public class FactorioModDependency {
    public FactorioModDependencyType Type { get; set; } = FactorioModDependencyType.HardRequirement;
    public required string Name { get; set; }
    public FactorioModDependencyEqualityVersion? EqualityVersion { get; set; }
    public bool IsOptional => Type is FactorioModDependencyType.Optional or FactorioModDependencyType.HiddenOptional;
    public bool IsIncompatible => Type is FactorioModDependencyType.Incompatibility;

    public bool AffectsSorting => Type 
        is FactorioModDependencyType.Optional 
        or FactorioModDependencyType.HiddenOptional
        or FactorioModDependencyType.HardRequirement;

    public bool IsSatisfied(Version version) {
        if (Type is FactorioModDependencyType.Incompatibility) {
            return false;
        }

        return EqualityVersion is null || EqualityVersion.IsSatisfied(version);
    }
}