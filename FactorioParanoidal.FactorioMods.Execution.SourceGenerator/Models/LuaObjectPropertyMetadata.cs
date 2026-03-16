namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;

internal record LuaObjectPropertyMetadata(
    string LuaName,
    string CSharpName,
    string FullyQualifiedTypeName
);