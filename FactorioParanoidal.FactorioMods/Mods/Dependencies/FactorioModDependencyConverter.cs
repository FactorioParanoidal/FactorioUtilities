using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FactorioParanoidal.FactorioMods.Mods.Dependencies;

public partial class FactorioModDependencyConverter : JsonConverter<FactorioModDependency> {
    public override FactorioModDependency? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options) {
        var stringValue = reader.GetString();
        if (stringValue is null) {
            throw new JsonException($"Can't parse null value as {typeToConvert}");
        }

        var parts = DependencyStringRegex()
            .Match(stringValue)
            .Groups
            .Values
            .Skip(1)
            .Where(group => group.Success)
            .Select(group => group.Value)
            .ToImmutableArray()
            .AsSpan();

        try {
            var dependencyType = ParsePrefix(ref parts);
            var dependencyName = ParseName(ref parts);
            var equalityVersion = ParseEqualityVersion(ref parts);

            if (parts.Length is not 0) {
                throw new JsonException("FactorioModDependency parsed, but here is unknown remaining data");
            }

            return new FactorioModDependency
                { Type = dependencyType, Name = dependencyName, EqualityVersion = equalityVersion };
        }
        catch (Exception e) when (e is not JsonException) {
            throw new JsonException($"Can't parse FactorioModDependency from {stringValue}", e);
        }
    }

    private FactorioModDependencyType ParsePrefix(ref ReadOnlySpan<string> parts) {
        FactorioModDependencyType? depType = parts[0] switch {
            "!" => FactorioModDependencyType.Incompatibility,
            "?" => FactorioModDependencyType.Optional,
            "(?)" => FactorioModDependencyType.HiddenOptional,
            "~" => FactorioModDependencyType.DoesNotAffectLoadOrder,
            _ => null
        };

        if (depType is not null) {
            parts = parts[1..];
        }

        return depType ?? FactorioModDependencyType.HardRequirement;
    }

    private string ParseName(ref ReadOnlySpan<string> parts) {
        var dependencyName = parts[0];
        parts = parts[1..];

        return dependencyName;
    }

    public FactorioModDependencyEqualityVersion? ParseEqualityVersion(ref ReadOnlySpan<string> parts) {
        if (parts.Length is 0) {
            return null;
        }

        var equality = parts[0] switch {
            "<" => FactorioModDependencyEquality.Less,
            "<=" => FactorioModDependencyEquality.LessOrEqual,
            "=" => FactorioModDependencyEquality.Equal,
            ">" => FactorioModDependencyEquality.Greater,
            ">=" => FactorioModDependencyEquality.GreaterOrEqual,
            _ => throw new JsonException($"Can't parse equality-operator from {parts[0]}")
        };
        var version = new Version(parts[1]);

        parts = parts[2..];
        return new FactorioModDependencyEqualityVersion() { Equality = equality, Version = version };
    }

    public override void Write(Utf8JsonWriter writer, FactorioModDependency value, JsonSerializerOptions options) {
        var type = value.Type switch {
            FactorioModDependencyType.HardRequirement => "",
            FactorioModDependencyType.Incompatibility => "! ",
            FactorioModDependencyType.Optional => "? ",
            FactorioModDependencyType.HiddenOptional => "(?) ",
            FactorioModDependencyType.DoesNotAffectLoadOrder => "~ ",
            _ => throw new ArgumentOutOfRangeException($"Unsupported FactorioModDependencyType type: {value.Type}")
        };

        var version = string.Empty;
        if (value.EqualityVersion is not null) {
            var equality = value.EqualityVersion.Equality switch {
                FactorioModDependencyEquality.Less => "<",
                FactorioModDependencyEquality.LessOrEqual => "<=",
                FactorioModDependencyEquality.Equal => "=",
                FactorioModDependencyEquality.Greater => ">",
                FactorioModDependencyEquality.GreaterOrEqual => ">=",
                _ => throw new JsonException($"Unsupported FactorioModDependencyEquality type: {value.EqualityVersion}")
            };

            version = $" {equality} {value.EqualityVersion.Version.ToString(3)}";
        }

        writer.WriteStringValue(type + value.Name + version);
    }

    [GeneratedRegex(@"(!|\?|\(\?\)|~)? *([a-zA-Z1-9-_]{1,100}) *(?:(<=|>=|=|<|>) *([\d]{1,5}.[\d]{1,5}(?:.[\d]{1,5})?))?")]
    private static partial Regex DependencyStringRegex();
}