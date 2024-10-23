using System.Collections.Immutable;
using System.Text.Json.Serialization;
using FactorioParanoidal.FactorioMods.Mods.Dependencies;

namespace FactorioParanoidal.FactorioMods.Mods;

public class FactorioModInfo {
    [JsonPropertyName("name")] public required string Name { get; set; }

    [VersionParts(3)]
    [JsonPropertyName("version")]
    public required Version Version { get; set; }

    [JsonPropertyName("title")] public required string Title { get; set; }

    [JsonPropertyName("author")] public required string Author { get; set; }

    [JsonPropertyName("contact")] public string? Contact { get; set; }

    [JsonPropertyName("homepage")] public string? Homepage { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [VersionParts(2)]
    [JsonPropertyName("factorio_version")]
    public Version? FactorioVersion { get; set; }

    [JsonPropertyName("dependencies")]
    public IList<FactorioModDependency> Dependencies { get; set; } = new List<FactorioModDependency>();

    public IReadOnlyList<FactorioModDependency> GetDependenciesWhatAffectLoadOrder() {
        return Dependencies.Where(dependency => dependency.Type
                is FactorioModDependencyType.Optional
                or FactorioModDependencyType.HardRequirement
                or FactorioModDependencyType.HiddenOptional)
            .ToImmutableArray();
    }
}