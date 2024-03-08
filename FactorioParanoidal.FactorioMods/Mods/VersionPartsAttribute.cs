using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Mods;

public class VersionPartsAttribute(int fieldsCount) : JsonConverterAttribute {
    public override JsonConverter? CreateConverter(Type typeToConvert) {
        return new VersionPartsConverter(fieldsCount);
    }

    private class VersionPartsConverter(int fieldCount) : JsonConverter<Version> {
        public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var s = reader.GetString();
            return s is null ? null : new Version(s);
        }

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString(fieldCount));
        }
    }
}