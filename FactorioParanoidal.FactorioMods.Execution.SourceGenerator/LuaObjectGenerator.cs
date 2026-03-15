using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator {
    [Generator]
    public class LuaObjectGenerator : IIncrementalGenerator {
        public void Initialize(IncrementalGeneratorInitializationContext context) {
            context.RegisterPostInitializationOutput(ctx => {
                ctx.AddSource("LuaExtraObjectAttribute.g.cs", SourceText.From(@"
using System;
namespace FactorioParanoidal.FactorioMods.Execution.SourceGenerator
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class LuaExtraObjectAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    internal class LuaExtraMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    internal class LuaExtraFieldsAttribute : Attribute { }
}", Encoding.UTF8));
            });

            var provider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);

            var compilationAndClasses = context.CompilationProvider.Combine(provider.Collect());

            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context) {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;

            foreach (var attributeListSyntax in typeDeclaration.AttributeLists) {
                foreach (var attributeSyntax in attributeListSyntax.Attributes) {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol) {
                        var attributeName = attributeSymbol.ContainingType.ToDisplayString();
                        if (attributeName ==
                            "FactorioParanoidal.FactorioMods.Execution.SourceGenerator.LuaExtraObjectAttribute") {
                            return typeDeclaration;
                        }
                    }
                }
            }

            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax?> classes,
            SourceProductionContext context) {
            if (classes.IsDefaultOrEmpty)
                return;

            var processedFileHints = new HashSet<string>();

            foreach (var classDeclaration in classes) {
                if (classDeclaration == null) continue;

                var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                    continue;

                // Use a consistent hint name for uniqueness check and file generation
                var fileHintName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", "")
                    .Replace("<", "_")
                    .Replace(">", "_");

                if (!processedFileHints.Add(fileHintName))
                    continue;

                GenerateLuaObjectCode(classSymbol, classDeclaration, context, fileHintName);
            }
        }

        private static void GenerateLuaObjectCode(INamedTypeSymbol classSymbol, TypeDeclarationSyntax classDeclaration,
            SourceProductionContext context, string fileHintName) {
            if (classSymbol.IsAbstract) {
                return;
            }

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            var properties = new List<(string LuaName, string CSharpName, ITypeSymbol Type)>();
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
                                properties.Add((luaName, prop.Name, prop.Type));
                            }
                        }

                        if (hasExtraFields && extraFieldsProperty == null) {
                            extraFieldsProperty = prop.Name;
                        }
                    }
                }

                currentType = currentType.BaseType;
            }

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    partial class {className} : global::Lua.ILuaUserData");
            sb.AppendLine("    {");

            // __index metamethod
            sb.AppendLine(
                "        static readonly global::Lua.LuaFunction __metamethod_index = new global::Lua.LuaFunction(\"index\", (context, ct) =>");
            sb.AppendLine("        {");
            sb.AppendLine($"            var userData = context.GetArgument<global::{namespaceName}.{className}>(0);");
            sb.AppendLine("            var key = context.GetArgument<global::System.String>(1);");
            sb.AppendLine("            switch (key)");
            sb.AppendLine("            {");
            foreach (var prop in properties) {
                sb.AppendLine($"                case \"{prop.LuaName}\":");
                sb.AppendLine(
                    $"                    return new global::System.Threading.Tasks.ValueTask<int>(context.Return(global::FactorioParanoidal.FactorioMods.Execution.Proxies.LuaValueUtility.ObjectToLuaValue(context.State, userData.{prop.CSharpName})));");
            }

            sb.AppendLine("                default:");
            if (extraFieldsProperty != null) {
                sb.AppendLine(
                    $"                    if (userData.{extraFieldsProperty}.TryGetValue(key, out var extraValue))");
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
            sb.AppendLine($"            var userData = context.GetArgument<global::{namespaceName}.{className}>(0);");
            sb.AppendLine("            var key = context.GetArgument<global::System.String>(1);");
            sb.AppendLine("            var value = context.GetArgument(2);");
            sb.AppendLine("            switch (key)");
            sb.AppendLine("            {");
            var fullyQualifiedFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

            int propIndex = 0;
            foreach (var prop in properties) {
                sb.AppendLine($"                case \"{prop.LuaName}\":");
                var typeStr = prop.Type.ToDisplayString(fullyQualifiedFormat);
                sb.AppendLine(
                    $"                    var propVal{propIndex} = global::FactorioParanoidal.FactorioMods.Execution.Proxies.LuaValueUtility.LuaValueToObject(value, typeof({typeStr}));");
                sb.AppendLine(
                    $"                    userData.{prop.CSharpName} = propVal{propIndex} == null ? default! : ({typeStr})propVal{propIndex};");
                sb.AppendLine("                    break;");
                propIndex++;
            }

            sb.AppendLine("                default:");
            if (extraFieldsProperty != null) {
                sb.AppendLine($"                    if (value == global::Lua.LuaValue.Nil)");
                sb.AppendLine($"                        userData.{extraFieldsProperty}.Remove(key);");
                sb.AppendLine($"                    else");
                sb.AppendLine(
                    $"                        userData.{extraFieldsProperty}[key] = global::FactorioParanoidal.FactorioMods.Execution.Proxies.LuaValueUtility.LuaValueToObject(value, typeof(object))!;");
                sb.AppendLine("                    break;");
            }
            else {
                sb.AppendLine(
                    $"                    throw new global::Lua.LuaRuntimeException(context.State, $\"'{{key}}' not found.\");");
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
            sb.AppendLine(
                "                __metatable[global::Lua.Runtime.Metamethods.NewIndex] = __metamethod_newindex;");
            sb.AppendLine("                return __metatable;");
            sb.AppendLine("            }");
            sb.AppendLine("            set { __metatable = value; }");
            sb.AppendLine("        }");

            sb.AppendLine("        public static implicit operator global::Lua.LuaValue(global::" + namespaceName +
                          "." + className + " value)");
            sb.AppendLine("        {");
            sb.AppendLine("            return global::Lua.LuaValue.FromUserData(value);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource($"{fileHintName}.LuaExtraObject.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}