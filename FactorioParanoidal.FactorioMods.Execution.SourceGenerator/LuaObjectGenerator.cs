using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using FactorioParanoidal.FactorioMods.Execution.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator;

[Generator]
public class LuaObjectGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(ctx => {
            ctx.AddSource("GeneratePrototypesAttribute.g.cs",
                SourceText.From("""

                                using System;
                                namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator
                                {
                                    [AttributeUsage(AttributeTargets.Assembly)]
                                    internal class GeneratePrototypesAttribute : Attribute
                                    {
                                        public string FileName { get; }
                                        public GeneratePrototypesAttribute(string fileName)
                                        {
                                            FileName = fileName;
                                        }
                                    }
                                }
                                """, Encoding.UTF8));
            ctx.AddSource("LuaExtraObjectAttribute.g.cs",
                SourceText.From("""

                                using System;
                                namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator
                                {
                                    [AttributeUsage(AttributeTargets.Class)]
                                    internal class LuaExtraObjectAttribute : Attribute { }

                                    [AttributeUsage(AttributeTargets.Property)]
                                    internal class LuaExtraMemberAttribute : Attribute { }

                                    [AttributeUsage(AttributeTargets.Property)]
                                    internal class LuaExtraFieldsAttribute : Attribute { }
                                }
                                """, Encoding.UTF8));
        });

        var attributeProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
                "FactorioParanoidal.FactorioMods.Execution.SourceGenerator.GeneratePrototypesAttribute",
                predicate: static (node, _) => true,
                transform: static (ctx, _) => ctx.Attributes.FirstOrDefault()?.ConstructorArguments.FirstOrDefault()
                    .Value?.ToString())
            .Where(static m => m is not null);

        var jsonContentProvider = context.AdditionalTextsProvider
            .Combine(attributeProvider.Collect())
            .SelectMany(static (pair, _) => {
                var file = pair.Left;
                var attributeValues = pair.Right;
                if (attributeValues.Contains(Path.GetFileName(file.Path))) {
                    return new[] { file };
                }

                return Array.Empty<AdditionalText>();
            })
            .Select(static (file, ct) => file.GetText(ct)?.ToString())
            .Where(static content => content is not null);

        var apiWithContext = jsonContentProvider.Select(static (json, _) => {
            try {
                var api = JsonSerializer.Deserialize<PrototypeApi>(json!);
                if (api == null) return null;

                var allTypeDefs = api.Types.ToImmutableDictionary(t => t.Name, t => t.Type ?? default);
                var generatedTypes = api.Prototypes.Select(p => p.Name)
                    .Union(api.Types.Where(t => t.Properties != null && t.Properties.Value.Length > 0)
                        .Select(t => t.Name))
                    .ToImmutableHashSet();

                var ctx = new SourceGenerationContext(
                    PrototypeGenerator.TypeMapping.ToImmutableDictionary(),
                    generatedTypes,
                    allTypeDefs
                );

                return new ApiAndContext(api, ctx);
            }
            catch {
                return null;
            }
        }).Where(static x => x is not null);

        var prototypeWorkItems = apiWithContext.SelectMany(static (x, _) => {
            return x!.Api.Prototypes.Select(p => new PrototypeWorkItem(p, x.Context));
        });

        var typeWorkItems = apiWithContext.SelectMany(static (x, _) => {
            return x!.Api.Types.Where(t => t.Properties != null && t.Properties.Value.Length > 0)
                .Select(t => new TypeWorkItem(t, x.Context));
        });

        context.RegisterSourceOutput(prototypeWorkItems, static (spc, item) => {
            var generator = new PrototypeGenerator();
            generator.GeneratePrototype(item.Prototype, item.Context, spc);
        });

        context.RegisterSourceOutput(typeWorkItems, static (spc, item) => {
            var generator = new PrototypeGenerator();
            generator.GenerateType(item.Type, item.Context, spc);
        });

        var luaObjectMetadataProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "FactorioParanoidal.FactorioMods.Execution.SourceGenerator.LuaExtraObjectAttribute",
            predicate: static (node, _) => node is ClassDeclarationSyntax,
            transform: static (ctx, _) => GetLuaObjectMetadata((INamedTypeSymbol)ctx.TargetSymbol)
        ).Where(static m => m is not null);

        context.RegisterSourceOutput(luaObjectMetadataProvider, static (spc, metadata) => {
            GenerateLuaObjectCode(metadata!, spc);
        });
    }

    private static LuaObjectMetadata? GetLuaObjectMetadata(INamedTypeSymbol classSymbol) {
        if (classSymbol.IsAbstract) return null;

        // Skip if the class is generated by our PrototypeGenerator
        if (classSymbol.ContainingNamespace.ToDisplayString() ==
            "FactorioParanoidal.FactorioMods.Execution.Prototypes") {
            if (classSymbol.Name != "GenericPrototype" && classSymbol.Name != "PrototypeBase") {
                return null;
            }
        }

        var properties = ImmutableArray.CreateBuilder<LuaObjectPropertyMetadata>();
        var seenLuaNames = new HashSet<string>();
        string? extraFieldsProperty = null;

        var currentType = classSymbol;
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object) {
            foreach (var member in currentType.GetMembers()) {
                if (member is IPropertySymbol prop) {
                    var hasLuaMember = prop.GetAttributes()
                        .Any(a => a.AttributeClass?.Name == "LuaExtraMemberAttribute");
                    var hasExtraFields = prop.GetAttributes()
                        .Any(a => a.AttributeClass?.Name == "LuaExtraFieldsAttribute");

                    if (hasLuaMember) {
                        var jsonPropAttr = prop.GetAttributes()
                            .FirstOrDefault(a => a.AttributeClass?.Name == "JsonPropertyNameAttribute");
                        string luaName = prop.Name.ToLowerInvariant();

                        if (jsonPropAttr != null && jsonPropAttr.ConstructorArguments.Length > 0) {
                            luaName = jsonPropAttr.ConstructorArguments[0].Value?.ToString() ?? luaName;
                        }

                        if (seenLuaNames.Add(luaName)) {
                            properties.Add(new LuaObjectPropertyMetadata(
                                luaName,
                                prop.Name,
                                prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            ));
                        }
                    }

                    if (hasExtraFields && extraFieldsProperty == null) {
                        extraFieldsProperty = prop.Name;
                    }
                }
            }

            currentType = currentType.BaseType;
        }

        return new LuaObjectMetadata(
            classSymbol.ContainingNamespace.ToDisplayString(),
            classSymbol.Name,
            properties.ToImmutable(),
            extraFieldsProperty
        );
    }

    private static void GenerateLuaObjectCode(LuaObjectMetadata metadata, SourceProductionContext context) {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine($"namespace {metadata.Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    partial class {metadata.ClassName} : global::Lua.ILuaUserData");
        sb.AppendLine("    {");

        // __index metamethod
        sb.AppendLine(
            "        static readonly global::Lua.LuaFunction __metamethod_index = new global::Lua.LuaFunction(\"index\", (context, ct) =>");
        sb.AppendLine("        {");
        sb.AppendLine(
            $"            var userData = context.GetArgument<global::{metadata.Namespace}.{metadata.ClassName}>(0);");
        sb.AppendLine("            var key = context.GetArgument<global::System.String>(1);");
        sb.AppendLine("            switch (key)");
        sb.AppendLine("            {");
        foreach (var prop in metadata.Properties) {
            sb.AppendLine($"                case \"{prop.LuaName}\":");
            sb.AppendLine(
                $"                    return new global::System.Threading.Tasks.ValueTask<int>(context.Return(global::FactorioParanoidal.FactorioMods.Execution.Proxies.LuaValueUtility.ObjectToLuaValue(context.State, userData.{prop.CSharpName})));");
        }

        sb.AppendLine("                default:");
        if (metadata.ExtraFieldsPropertyName != null) {
            sb.AppendLine(
                $"                    if (userData.{metadata.ExtraFieldsPropertyName}.TryGetValue(key, out var extraValue))");
            sb.AppendLine(
                $"                        return new global::System.Threading.Tasks.ValueTask<int>(context.Return(global::FactorioParanoidal.FactorioMods.Execution.Proxies.LuaValueUtility.ObjectToLuaValue(context.State, extraValue)));");
        }

        sb.AppendLine(
            "                    return new global::System.Threading.Tasks.ValueTask<int>(context.Return(global::Lua.LuaValue.Nil));");
        sb.AppendLine("            }");
        sb.AppendLine("        });");

        // __newindex metamethod
        sb.AppendLine(
            "        static readonly global::Lua.LuaFunction __metamethod_newindex = new global::Lua.LuaFunction(\"newindex\", (context, ct) =>");
        sb.AppendLine("        {");
        sb.AppendLine(
            $"            var userData = context.GetArgument<global::{metadata.Namespace}.{metadata.ClassName}>(0);");
        sb.AppendLine("            var key = context.GetArgument<global::System.String>(1);");
        sb.AppendLine("            var value = context.GetArgument(2);");
        sb.AppendLine("            switch (key)");
        sb.AppendLine("            {");

        int propIndex = 0;
        foreach (var prop in metadata.Properties) {
            sb.AppendLine($"                case \"{prop.LuaName}\":");
            sb.AppendLine(
                $"                    var propVal{propIndex} = global::FactorioParanoidal.FactorioMods.Execution.Proxies.LuaValueUtility.LuaValueToObject(value, typeof({prop.FullyQualifiedTypeName}));");
            sb.AppendLine(
                $"                    userData.{prop.CSharpName} = propVal{propIndex} == null ? default! : ({prop.FullyQualifiedTypeName})propVal{propIndex};");
            sb.AppendLine("                    break;");
            propIndex++;
        }

        sb.AppendLine("                default:");
        if (metadata.ExtraFieldsPropertyName != null) {
            sb.AppendLine($"                    if (value == global::Lua.LuaValue.Nil)");
            sb.AppendLine($"                        userData.{metadata.ExtraFieldsPropertyName}.Remove(key);");
            sb.AppendLine($"                    else");
            sb.AppendLine(
                $"                        userData.{metadata.ExtraFieldsPropertyName}[key] = global::FactorioParanoidal.FactorioMods.Execution.Proxies.LuaValueUtility.LuaValueToObject(value, typeof(object))!;");
            sb.AppendLine("                    break;");
        }
        else {
            sb.AppendLine(
                "                    throw new global::Lua.LuaRuntimeException(context.State, $\"'{key}' not found.\");");
        }

        sb.AppendLine("            }");
        sb.AppendLine("            return new global::System.Threading.Tasks.ValueTask<int>(context.Return());");
        sb.AppendLine("        });");

        // Metatable property
        sb.AppendLine("        static global::Lua.LuaTable? __metatable;");
        sb.AppendLine("        global::Lua.LuaTable? global::Lua.ILuaUserData.Metatable");
        sb.AppendLine("        {");
        sb.AppendLine("            get");
        sb.AppendLine("            {");
        sb.AppendLine("                if (__metatable != null) return __metatable;");
        sb.AppendLine("                __metatable = new();");
        sb.AppendLine("                __metatable[global::Lua.Runtime.Metamethods.Index] = __metamethod_index;");
        sb.AppendLine("                __metatable[global::Lua.Runtime.Metamethods.NewIndex] = __metamethod_newindex;");
        sb.AppendLine("                return __metatable;");
        sb.AppendLine("            }");
        sb.AppendLine("            set { __metatable = value; }");
        sb.AppendLine("        }");

        sb.AppendLine(
            $"        public static implicit operator global::Lua.LuaValue(global::{metadata.Namespace}.{metadata.ClassName} value)");
        sb.AppendLine("        {");
        sb.AppendLine("            return global::Lua.LuaValue.FromUserData(value);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var fileHintName = $"{metadata.Namespace}.{metadata.ClassName}".Replace("<", "_").Replace(">", "_");
        context.AddSource($"{fileHintName}.LuaExtraObject.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}