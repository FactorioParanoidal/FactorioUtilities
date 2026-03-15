using Lua;

namespace FactorioParanoidal.FactorioMods.Execution;

public class PrototypeRegistry
{
    // type -> name -> prototype
    public Dictionary<string, Dictionary<string, LuaValue>> Prototypes { get; } = new();

    public void Extend(LuaTable table)
    {
        foreach (var entry in table)
        {
            if (entry.Value.TryRead<LuaTable>(out var prototype))
            {
                var typeValue = prototype["type"];
                var nameValue = prototype["name"];

                if (typeValue.TryRead<string>(out var type) && nameValue.TryRead<string>(out var name))
                {
                    if (!Prototypes.TryGetValue(type, out var typeDict))
                    {
                        typeDict = new Dictionary<string, LuaValue>();
                        Prototypes[type] = typeDict;
                    }
                    typeDict[name] = entry.Value;
                }
            }
        }
    }

    public LuaTable ToLuaTable(LuaState state)
    {
        var root = new LuaTable();
        foreach (var typeEntry in Prototypes)
        {
            var typeTable = new LuaTable();
            foreach (var nameEntry in typeEntry.Value)
            {
                typeTable[nameEntry.Key] = nameEntry.Value;
            }
            root[typeEntry.Key] = typeTable;
        }
        return root;
    }
}
