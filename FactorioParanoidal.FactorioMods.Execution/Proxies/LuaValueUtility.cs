using Lua;

namespace FactorioParanoidal.FactorioMods.Execution.Proxies;

public static class LuaValueUtility {
    public static object? LuaValueToObject(LuaValue value, Type targetType) {
        if (value == LuaValue.Nil) return null;

        if (targetType == typeof(string))
            return value.TryRead<string>(out var str) ? str : null;

        if (targetType == typeof(int))
            return value.TryRead<double>(out var d) ? (int)d : 0;

        if (targetType == typeof(double))
            return value.TryRead<double>(out var d2) ? d2 : 0.0;

        if (targetType == typeof(bool))
            return value.TryRead<bool>(out var b) && b;

        if (targetType == typeof(object)) {
            if (value.TryRead<string>(out var strVal)) return strVal;
            if (value.TryRead<double>(out var dblVal)) return dblVal;
            if (value.TryRead<bool>(out var boolVal)) return boolVal;
            // Return raw value for complex types if we just want 'object'
            return value;
        }

        // For arrays/lists, we'd need more complex logic.
        // For now, if we don't know the type, we just try to return the raw LuaValue or null.
        return null;
    }

    public static LuaValue ObjectToLuaValue(LuaState state, object? obj) {
        if (obj == null) return LuaValue.Nil;

        if (obj is string str) return LuaValue.FromObject(str);
        if (obj is int i) return LuaValue.FromObject((double)i);
        if (obj is double d) return LuaValue.FromObject(d);
        if (obj is bool b) return LuaValue.FromObject(b);
        if (obj is LuaValue lv) return lv;

        // Fallback
        return LuaValue.FromObject(obj.ToString() ?? "");
    }
}