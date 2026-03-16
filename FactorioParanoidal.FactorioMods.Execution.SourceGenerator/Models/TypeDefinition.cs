using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

internal record TypeDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("description")]
    string Description,
    [property: JsonPropertyName("parent")] string? Parent,
    [property: JsonPropertyName("abstract")]
    bool Abstract,
    [property: JsonPropertyName("properties")]
    ImmutableArray<Property>? Properties,
    [property: JsonPropertyName("type")] SerializableJsonElement? Type
);