using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactorioParanoidal.Generator.Prototypes.Models;

public class PrototypeApi {
    [JsonPropertyName("application")] public string Application { get; set; } = string.Empty;

    [JsonPropertyName("application_version")]
    public string ApplicationVersion { get; set; } = string.Empty;

    [JsonPropertyName("api_version")] public int ApiVersion { get; set; }

    [JsonPropertyName("stage")] public string Stage { get; set; } = string.Empty;

    [JsonPropertyName("prototypes")] public List<Prototype> Prototypes { get; set; } = new();

    [JsonPropertyName("types")] public List<TypeDefinition> Types { get; set; } = new();
}

public class Prototype {
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")] public int Order { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent")] public string? Parent { get; set; }

    [JsonPropertyName("abstract")] public bool Abstract { get; set; }

    [JsonPropertyName("properties")] public List<Property> Properties { get; set; } = new();

    [JsonPropertyName("custom_properties")]
    public JsonElement? CustomProperties { get; set; }
}

public class TypeDefinition {
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")] public int Order { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent")] public string? Parent { get; set; }

    [JsonPropertyName("abstract")] public bool Abstract { get; set; }

    [JsonPropertyName("properties")] public List<Property>? Properties { get; set; }

    [JsonPropertyName("type")] public JsonElement? Type { get; set; }
}

public class Property {
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")] public int Order { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("optional")] public bool Optional { get; set; }

    [JsonPropertyName("type")] public JsonElement Type { get; set; }

    [JsonPropertyName("default")] public JsonElement? Default { get; set; }
}

public class ComplexType {
    [JsonPropertyName("complex_type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("value")] public JsonElement? Value { get; set; }

    [JsonPropertyName("options")] public List<JsonElement>? Options { get; set; }

    [JsonPropertyName("attributes")] public List<JsonElement>? Attributes { get; set; }

    [JsonPropertyName("parameters")] public List<JsonElement>? Parameters { get; set; }

    [JsonPropertyName("key")] public JsonElement? Key { get; set; }

    [JsonPropertyName("item")] public JsonElement? Item { get; set; }
}