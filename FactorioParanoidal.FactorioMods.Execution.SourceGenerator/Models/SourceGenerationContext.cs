using System.Collections.Immutable;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

internal record SourceGenerationContext(
    ImmutableDictionary<string, string> TypeMapping,
    ImmutableHashSet<string> GeneratedTypes,
    ImmutableDictionary<string, SerializableJsonElement> AllTypeDefs
);