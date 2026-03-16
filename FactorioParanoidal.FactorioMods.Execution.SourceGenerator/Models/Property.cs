using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

internal record Property(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("description")]
    string Description,
    [property: JsonPropertyName("optional")]
    bool Optional,
    [property: JsonPropertyName("type")] SerializableJsonElement Type,
    [property: JsonPropertyName("default")]
    SerializableJsonElement? Default
);