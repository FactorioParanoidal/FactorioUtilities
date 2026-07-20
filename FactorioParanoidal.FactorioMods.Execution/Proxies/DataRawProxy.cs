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
                if (!registry.HasType(type)) return context.Return(LuaValue.Nil);

                return context.Return(CreatePrototypeTypeProxy(state, registry, type));
            });

        mt["__pairs"] = new LuaFunction((context, _) => {
            var snapshot = new LuaTable();
            foreach (var type in registry.Prototypes.Keys) {
                snapshot[type] = CreatePrototypeTypeProxy(state, registry, type);
            }

            return new ValueTask<int>(ReturnPairs(context, snapshot));
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

        mt["__pairs"] = new LuaFunction((context, _) => {
            var snapshot = new LuaTable();
            if (registry.Prototypes.TryGetValue(type, out var prototypes)) {
                foreach (var name in prototypes.Keys) snapshot[name] = registry.GetRawTable(type, name)!;
            }

            return new ValueTask<int>(ReturnPairs(context, snapshot));
        });

        table.Metatable = mt;
        return table;
    }

    private static int ReturnPairs(LuaFunctionExecutionContext context, LuaTable snapshot) {
        var entries = snapshot.ToArray();
        var index = 0;
        var iterator = new LuaFunction((iteratorContext, _) => {
            if (index >= entries.Length)
                return new ValueTask<int>(iteratorContext.Return(LuaValue.Nil));

            var entry = entries[index++];
            return new ValueTask<int>(iteratorContext.Return(entry.Key, entry.Value));
        });
        return context.Return(iterator, snapshot, LuaValue.Nil);
    }
}