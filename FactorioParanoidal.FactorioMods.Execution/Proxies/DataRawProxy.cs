using System.Reflection;
using FactorioParanoidal.Models.Prototypes;
using Lua;

namespace FactorioParanoidal.FactorioMods.Execution.Proxies;

public static class DataRawProxy {
    public static LuaTable Create(LuaState state, PrototypeRegistry registry) {
        var table = new LuaTable();
        var mt = new LuaTable();

        mt["__index"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount < 2) return context.Return(LuaValue.Nil);
                if (!context.GetArgument(1).TryRead<string>(out var type)) return context.Return(LuaValue.Nil);

                return context.Return(CreatePrototypeTypeProxy(state, registry, type));
            });

        mt["__newindex"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                throw new Exception("Cannot modify data.raw directly; use data:extend or modify specific prototypes.");
            });

        table.Metatable = mt;
        return table;
    }

    public static LuaTable CreatePrototypeTypeProxy(LuaState state, PrototypeRegistry registry, string type) {
        var table = new LuaTable();
        var mt = new LuaTable();

        mt["__index"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount < 2) return context.Return(LuaValue.Nil);
                if (!context.GetArgument(1).TryRead<string>(out var name)) return context.Return(LuaValue.Nil);

                var prototype = registry.GetPrototype(type, name);
                if (prototype != null) {
                    return context.Return(CreatePrototypeProxy(state, prototype));
                }

                return context.Return(LuaValue.Nil);
            });

        mt["__newindex"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount < 3) return context.Return();
                if (!context.GetArgument(1).TryRead<string>(out var name)) return context.Return();

                var valueArg = context.GetArgument(2);
                if (valueArg.TryRead<LuaTable>(out var protoTable)) {
                    // Register and create the C# object
                    registry.ConvertAndRegister(type, name, protoTable);
                }
                else {
                    // If they set to nil, we might want to remove it
                    if (valueArg == LuaValue.Nil) {
                        registry.RemovePrototype(type, name);
                    }
                    else {
                        throw new Exception($"Expected table or nil for data.raw['{type}']['{name}']");
                    }
                }

                return context.Return();
            });

        table.Metatable = mt;
        return table;
    }

    public static LuaTable CreatePrototypeProxy(LuaState state, FactorioPrototype obj) {
        var table = new LuaTable();
        var mt = new LuaTable();

        mt["__index"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount < 2) return context.Return(LuaValue.Nil);
                if (!context.GetArgument(1).TryRead<string>(out var key)) return context.Return(LuaValue.Nil);

                // 1. Try Reflection
                var prop = obj.GetType().GetProperty(key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null) {
                    var val = prop.GetValue(obj);
                    return context.Return(LuaValueUtility.ObjectToLuaValue(state, val));
                }

                // 2. Try ExtraFields
                if (obj.ExtraFields.TryGetValue(key, out var extraVal)) {
                    return context.Return(LuaValueUtility.ObjectToLuaValue(state, extraVal));
                }

                return context.Return(LuaValue.Nil);
            });

        mt["__newindex"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount < 3) return context.Return();
                if (!context.GetArgument(1).TryRead<string>(out var key)) return context.Return();
                var valArg = context.GetArgument(2);

                // 1. Try Reflection Setter
                var prop = obj.GetType().GetProperty(key,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite) {
                    var typedVal = LuaValueUtility.LuaValueToObject(valArg, prop.PropertyType);
                    prop.SetValue(obj, typedVal);
                }
                else {
                    // 2. Fallback to ExtraFields
                    if (valArg == LuaValue.Nil) {
                        obj.ExtraFields.Remove(key);
                    }
                    else {
                        obj.ExtraFields[key] = LuaValueUtility.LuaValueToObject(valArg, typeof(object))!;
                    }
                }

                return context.Return();
            });

        table.Metatable = mt;
        return table;
    }
}