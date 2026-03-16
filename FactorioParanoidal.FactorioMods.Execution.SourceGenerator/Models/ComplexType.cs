using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

internal record ComplexType(
    [property: JsonPropertyName("complex_type")]
    string Type,
    [property: JsonPropertyName("value")] SerializableJsonElement? Value,
    [property: JsonPropertyName("options")]
    ImmutableArray<SerializableJsonElement>? Options,
    [property: JsonPropertyName("attributes")]
    ImmutableArray<SerializableJsonElement>? Attributes,
    [property: JsonPropertyName("parameters")]
    ImmutableArray<SerializableJsonElement>? Parameters,
    [property: JsonPropertyName("key")] SerializableJsonElement? Key,
    [property: JsonPropertyName("item")] SerializableJsonElement? Item
);