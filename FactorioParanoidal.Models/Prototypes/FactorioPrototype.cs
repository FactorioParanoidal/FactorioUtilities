using System.Text.Json.Serialization;

namespace FactorioParanoidal.Models.Prototypes;

public abstract class FactorioPrototype {
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    // We use object to store generic dynamic values coming from Lua that don't map to typed properties
    [JsonExtensionData] public Dictionary<string, object> ExtraFields { get; set; } = new();
}