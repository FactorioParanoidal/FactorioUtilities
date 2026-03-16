using System.Collections.Immutable;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

internal record LuaObjectMetadata(
    string Namespace,
    string ClassName,
    ImmutableArray<LuaObjectPropertyMetadata> Properties,
    string? ExtraFieldsPropertyName
);