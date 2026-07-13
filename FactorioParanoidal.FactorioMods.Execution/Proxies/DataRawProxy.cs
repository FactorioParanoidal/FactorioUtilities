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

                var raw = registry.GetRawTable(type, name);
                return context.Return(raw is not null ? raw : LuaValue.Nil);
            });

        mt["__newindex"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount < 3) return context.Return();
                if (!context.GetArgument(1).TryRead<string>(out var name)) return context.Return();

                var valueArg = context.GetArgument(2);
                if (valueArg.TryRead<LuaTable>(out var protoTable)) {
                    registry.ConvertAndRegister(type, name, protoTable);
                }
                else if (valueArg == LuaValue.Nil) {
                    registry.RemovePrototype(type, name);
                }
                else {
                    throw new Exception($"Expected table or nil for data.raw['{type}']['{name}']");
                }

                return context.Return();
            });

        table.Metatable = mt;
        return table;
    }
}