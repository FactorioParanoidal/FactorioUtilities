using System.Reflection;
using System.Text.Json.Serialization;
using FactorioParanoidal.FactorioMods.Execution.Prototypes;
using FactorioParanoidal.FactorioMods.Execution.Proxies;
using Lua;

namespace FactorioParanoidal.FactorioMods.Execution;

public class PrototypeRegistry {
    // type -> name -> prototype
    public Dictionary<string, Dictionary<string, FactorioPrototype>> Prototypes { get; } = new();

    public void ConvertAndRegister(string type, string name, LuaTable prototypeTable) {
        var obj = CreatePrototypeInstance(type);
        obj.Type = type;
        obj.Name = name;

        PopulateFromLuaTable(obj, prototypeTable);

        if (!Prototypes.TryGetValue(type, out var typeDict)) {
            typeDict = new Dictionary<string, FactorioPrototype>();
            Prototypes[type] = typeDict;
        }

        typeDict[name] = obj;
    }

    public FactorioPrototype? GetPrototype(string type, string name) {
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

    private FactorioPrototype CreatePrototypeInstance(string type) {
        // Simple factory logic. Can be expanded via reflection later.
        if (type == "item") return new ItemPrototype();

        // Fallback generic prototype if specific class doesn't exist
        return new FactorioPrototype();
    }

    private void PopulateFromLuaTable(FactorioPrototype obj, LuaTable table) {
        var properties = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        foreach (var entry in table) {
            if (!entry.Key.TryRead<string>(out var keyStr)) continue;

            if (keyStr == "type" || keyStr == "name") continue;

            var prop = Array.Find(properties, p => p.Name.Equals(keyStr, StringComparison.OrdinalIgnoreCase) ||
                                                   (p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == keyStr));

            if (prop != null && prop.CanWrite) {
                var val = LuaValueUtility.LuaValueToObject(entry.Value, prop.PropertyType);
                prop.SetValue(obj, val);
            }
            else {
                obj.ExtraFields[keyStr] = LuaValueUtility.LuaValueToObject(entry.Value, typeof(object))!;
            }
        }
    }
}