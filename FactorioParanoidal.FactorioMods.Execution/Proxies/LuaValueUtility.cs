using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
using FactorioParanoidal.FactorioMods.Execution.SourceGenerator;
using Lua;

namespace FactorioParanoidal.FactorioMods.Execution.Proxies;

public static class LuaValueUtility {
    public static object? LuaValueToObject(LuaValue value, Type targetType) {
        if (value == LuaValue.Nil) return null;

        // Handle Nullable<T>
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var effectiveType = underlyingType ?? targetType;

        if (effectiveType == typeof(string))
            return value.TryRead<string>(out var str) ? str : null;

        if (effectiveType == typeof(int))
            return value.TryRead<double>(out var d) ? (int)Math.Round(d) : 0;

        if (effectiveType == typeof(uint))
            return value.TryRead<double>(out var d) ? (uint)Math.Round(d) : 0u;

        if (effectiveType == typeof(short))
            return value.TryRead<double>(out var d) ? (short)Math.Round(d) : (short)0;

        if (effectiveType == typeof(ushort))
            return value.TryRead<double>(out var d) ? (ushort)Math.Round(d) : (ushort)0;

        if (effectiveType == typeof(byte))
            return value.TryRead<double>(out var d) ? (byte)Math.Round(d) : (byte)0;

        if (effectiveType == typeof(sbyte))
            return value.TryRead<double>(out var d) ? (sbyte)Math.Round(d) : (sbyte)0;

        if (effectiveType == typeof(long))
            return value.TryRead<double>(out var d) ? (long)Math.Round(d) : 0L;

        if (effectiveType == typeof(ulong))
            return value.TryRead<double>(out var d) ? (ulong)Math.Round(d) : 0UL;

        if (effectiveType == typeof(float))
            return value.TryRead<double>(out var d) ? (float)d : 0.0f;

        if (effectiveType == typeof(double))
            return value.TryRead<double>(out var d2) ? d2 : 0.0;

        if (effectiveType == typeof(bool))
            return value.TryRead<bool>(out var b) && b;

        if (effectiveType == typeof(object)) {
            if (value.TryRead<string>(out var strVal)) return strVal;
            if (value.TryRead<double>(out var dblVal)) return dblVal;
            if (value.TryRead<bool>(out var boolVal)) return boolVal;
            // Return raw value for complex types if we just want 'object'
            return value;
        }

        // Handle complex types (classes/structs) from LuaTable
        if (value.TryRead<LuaTable>(out var table)) {
            // Special case: List<T>
            if (effectiveType.IsGenericType && effectiveType.GetGenericTypeDefinition() == typeof(List<>)) {
                var itemType = effectiveType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(effectiveType)!;
                foreach (var entry in table) {
                    list.Add(LuaValueToObject(entry.Value, itemType));
                }

                return list;
            }

            // Special case: Dictionary<K, V>
            if (effectiveType.IsGenericType && effectiveType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                var keyType = effectiveType.GetGenericArguments()[0];
                var valueType = effectiveType.GetGenericArguments()[1];
                var dict = (IDictionary)Activator.CreateInstance(effectiveType)!;
                foreach (var entry in table) {
                    dict.Add(LuaValueToObject(entry.Key, keyType)!, LuaValueToObject(entry.Value, valueType));
                }

                return dict;
            }

            // General case: Create instance and populate
            try {
                var instance = Activator.CreateInstance(effectiveType);
                if (instance != null) {
                    PopulateObjectFromTable(instance, table);
                    return instance;
                }
            }
            catch {
                // If we can't create an instance, return the table as object or null
                return table;
            }
        }

        return null;
    }

    public static void PopulateObjectFromTable(object obj, LuaTable table) {
        var properties = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        // Check for ExtraFields property
        var extraFieldsProp = Array.Find(properties, p => p.GetCustomAttribute<LuaExtraFieldsAttribute>() != null);
        var extraFields = extraFieldsProp?.GetValue(obj) as Dictionary<string, object>;

        foreach (var entry in table) {
            if (!entry.Key.TryRead<string>(out var keyStr)) continue;

            // Skip common metadata
            if (keyStr == "type" || keyStr == "name") {
                // We might still want to populate them if they exist in the target object
            }

            var prop = Array.Find(properties, p => p.Name.Equals(keyStr, StringComparison.OrdinalIgnoreCase) ||
                                                   (p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == keyStr));

            if (prop != null && prop.CanWrite) {
                var val = LuaValueToObject(entry.Value, prop.PropertyType);
                prop.SetValue(obj, val);
            }
            else if (extraFields != null) {
                extraFields[keyStr] = LuaValueToObject(entry.Value, typeof(object))!;
            }
        }
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