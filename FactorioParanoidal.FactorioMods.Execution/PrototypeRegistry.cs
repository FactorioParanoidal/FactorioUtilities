using System.Reflection;
using FactorioParanoidal.FactorioMods.Execution.Prototypes;
using FactorioParanoidal.FactorioMods.Execution.Proxies;
using Lua;

namespace FactorioParanoidal.FactorioMods.Execution;

public class PrototypeRegistry {
    private static readonly Dictionary<string, Type> TypeCache = new(StringComparer.OrdinalIgnoreCase);

    // type -> name -> prototype
    public Dictionary<string, Dictionary<string, PrototypeBase>> Prototypes { get; } = new();

    public void ConvertAndRegister(string type, string name, LuaTable prototypeTable) {
        var obj = CreatePrototypeInstance(type);
        obj.Type = type;
        obj.Name = name;

        LuaValueUtility.PopulateObjectFromTable(obj, prototypeTable);

        if (!Prototypes.TryGetValue(type, out var typeDict)) {
            typeDict = new Dictionary<string, PrototypeBase>();
            Prototypes[type] = typeDict;
        }

        typeDict[name] = obj;
    }

    public PrototypeBase? GetPrototype(string type, string name) {
        if (Prototypes.TryGetValue(type, out var typeDict) && typeDict.TryGetValue(name, out var prototype)) {
            return prototype;
        }

        return null;
    }

    public void RemovePrototype(string type, string name) {
        if (Prototypes.TryGetValue(type, out var typeDict)) {
            typeDict.Remove(name);
        }
    }

    public void Extend(LuaTable table) {
        foreach (var entry in table) {
            if (entry.Value.TryRead<LuaTable>(out var prototype)) {
                var typeValue = prototype["type"];
                var nameValue = prototype["name"];

                if (typeValue.TryRead<string>(out var type) && nameValue.TryRead<string>(out var name)) {
                    ConvertAndRegister(type, name, prototype);
                }
            }
        }
    }

    private PrototypeBase CreatePrototypeInstance(string type) {
        if (TypeCache.TryGetValue(type, out var cachedType)) {
            return (PrototypeBase)Activator.CreateInstance(cachedType)!;
        }

        // Convert snake-case to PascalCase and append Prototype
        var className = SnakeToPascal(type) + "Prototype";

        // Search in the prototypes namespace
        var prototypeType = Assembly.GetExecutingAssembly().GetTypes()
            .FirstOrDefault(t => t.Namespace == "FactorioParanoidal.FactorioMods.Execution.Prototypes" &&
                                 t.Name.Equals(className, StringComparison.OrdinalIgnoreCase));

        if (prototypeType != null) {
            TypeCache[type] = prototypeType;
            return (PrototypeBase)Activator.CreateInstance(prototypeType)!;
        }

        // Fallback generic prototype if specific class doesn't exist
        return new GenericPrototype();
    }

    private static string SnakeToPascal(string snake) {
        if (string.IsNullOrEmpty(snake)) return snake;
        var words = snake.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++) {
            if (words[i].Length > 0) {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
        }

        return string.Join("", words);
    }
}