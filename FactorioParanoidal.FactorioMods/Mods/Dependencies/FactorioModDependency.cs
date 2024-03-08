using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Mods.Dependencies;

[JsonConverter(typeof(FactorioModDependencyConverter))]
public class FactorioModDependency {
    public FactorioModDependencyType Type { get; set; } = FactorioModDependencyType.HardRequirement;
    public required string Name { get; set; }
    public FactorioModDependencyEqualityVersion? EqualityVersion { get; set; }
}