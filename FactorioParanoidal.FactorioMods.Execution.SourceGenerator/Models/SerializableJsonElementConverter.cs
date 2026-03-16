using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

internal class SerializableJsonElementConverter : JsonConverter<SerializableJsonElement> {
    public override SerializableJsonElement Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options) {
        using var doc = JsonDocument.ParseValue(ref reader);
        return new SerializableJsonElement(doc.RootElement.GetRawText(), doc.RootElement.ValueKind);
    }

    public override void Write(Utf8JsonWriter writer, SerializableJsonElement value, JsonSerializerOptions options) {
        writer.WriteRawValue(value.RawJson);
    }
}