using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

[JsonConverter(typeof(SerializableJsonElementConverter))]
internal record struct SerializableJsonElement(string RawJson, JsonValueKind ValueKind) {
    public JsonElement ToJsonElement() {
        if (string.IsNullOrEmpty(RawJson)) return default;
        using var doc = JsonDocument.Parse(RawJson);
        return doc.RootElement.Clone();
    }

    public string? GetString() => ValueKind == JsonValueKind.String ? ToJsonElement().GetString() : null;

    public override string ToString() => RawJson;
}