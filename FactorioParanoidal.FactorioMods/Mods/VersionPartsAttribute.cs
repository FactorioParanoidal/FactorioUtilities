using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.Mods;

public class VersionPartsAttribute(int fieldsCount) : JsonConverterAttribute {
    public override JsonConverter? CreateConverter(Type typeToConvert) {
        if (typeToConvert == typeof(Version)) {
            return new VersionPartsConverter(fieldsCount);
        }

        if (typeToConvert == typeof(FactorioVersion)) {
            return new FactorioVersionPartsConverter(fieldsCount);
        }

        throw new InvalidOperationException($"{nameof(VersionPartsAttribute)} cannot convert {typeToConvert}");
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

    private class FactorioVersionPartsConverter(int fieldCount) : JsonConverter<FactorioVersion> {
        public override FactorioVersion? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options) {
            var value = reader.GetString();
            return value is null ? null : new FactorioVersion(value);
        }

        public override void Write(Utf8JsonWriter writer, FactorioVersion value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString(fieldCount));
        }
    }
}