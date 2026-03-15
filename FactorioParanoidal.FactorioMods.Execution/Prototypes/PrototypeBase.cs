using System.Text.Json.Serialization;
using FactorioParanoidal.FactorioMods.Execution.Proxies;
using Lua;
using Lua.Runtime;

namespace FactorioParanoidal.FactorioMods.Execution.Prototypes;

public abstract partial class PrototypeBase : ILuaUserData {
    static readonly LuaFunction __metamethod_index = new LuaFunction("index", (context, ct) => {
        var userData = context.GetArgument<PrototypeBase>(0);
        var key = context.GetArgument<String>(1);
        switch (key) {
            default:
                if (userData.ExtraFields.TryGetValue(key, out var extraValue))
                    return new ValueTask<int>(
                        context.Return(LuaValueUtility.ObjectToLuaValue(context.State, extraValue)));
                return new ValueTask<int>(context.Return(LuaValue.Nil));
        }
    });

    static readonly LuaFunction __metamethod_newindex = new LuaFunction("newindex", (context, ct) => {
        var userData = context.GetArgument<PrototypeBase>(0);
        var key = context.GetArgument<String>(1);
        var value = context.GetArgument(2);
        switch (key) {
            default:
                if (value == LuaValue.Nil)
                    userData.ExtraFields.Remove(key);
                else
                    userData.ExtraFields[key] = LuaValueUtility.LuaValueToObject(value, typeof(object))!;
                break;
        }

        return new ValueTask<int>(context.Return());
    });

    static LuaTable? __metatable;

    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    // We use object to store generic dynamic values coming from Lua that don't map to typed properties
    public Dictionary<string, object> ExtraFields { get; set; } = new();

    LuaTable? ILuaUserData.Metatable {
        get {
            if (__metatable != null) return __metatable;
            __metatable = new();
            __metatable[Metamethods.Index] = __metamethod_index;
            __metatable[Metamethods.NewIndex] = __metamethod_newindex;
            return __metatable;
        }
        set { __metatable = value; }
    }

    public static implicit operator LuaValue(PrototypeBase value) {
        return LuaValue.FromUserData(value);
    }
}