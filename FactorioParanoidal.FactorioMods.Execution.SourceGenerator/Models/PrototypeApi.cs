using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

internal record PrototypeApi(
    [property: JsonPropertyName("application")]
    string Application,
    [property: JsonPropertyName("application_version")]
    string ApplicationVersion,
    [property: JsonPropertyName("api_version")]
    int ApiVersion,
    [property: JsonPropertyName("stage")] string Stage,
    [property: JsonPropertyName("prototypes")]
    ImmutableArray<Prototype> Prototypes,
    [property: JsonPropertyName("types")] ImmutableArray<TypeDefinition> Types
);