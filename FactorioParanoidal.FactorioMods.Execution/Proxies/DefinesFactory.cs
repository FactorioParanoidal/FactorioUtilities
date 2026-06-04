using System.Text.Json;
using Lua;

namespace FactorioParanoidal.FactorioMods.Execution.Proxies;

/// <summary>
///     Builds the engine-injected <c>defines</c> global table that Factorio exposes to every Lua stage
///     (e.g. <c>defines.direction.north</c>, <c>defines.inventory.*</c>). Mods read and extend it during the data
///     stage, so it must exist before any mod code runs.
///     The structure is generated from Factorio's published <c>runtime-api.json</c> (embedded as <c>defines.json</c>):
///     each define is either a leaf set of named values (where <c>order</c> is the numeric value) or a tree of nested
///     subkeys.
/// </summary>
public static class DefinesFactory {
    private const string ResourceSuffix = "Data.defines.json";

    public static LuaTable Create() {
        using var stream = OpenResource();
        using var document = JsonDocument.Parse(stream);

        var defines = new LuaTable();
        if (document.RootElement.TryGetProperty("defines", out var definesArray)) {
            foreach (var define in definesArray.EnumerateArray()) {
                var name = define.GetProperty("name").GetString()!;
                defines[name] = BuildNode(define);
            }
        }

        return defines;
    }

    private static LuaValue BuildNode(JsonElement node) {
        var table = new LuaTable();

        // Leaf node: a flat set of { name, order } entries, where order is the value the engine assigns.
        if (node.TryGetProperty("values", out var values)) {
            foreach (var value in values.EnumerateArray()) {
                var valueName = value.GetProperty("name").GetString()!;
                table[valueName] = value.GetProperty("order").GetInt64();
            }
        }

        // Branch node: nested define groups.
        if (node.TryGetProperty("subkeys", out var subkeys)) {
            foreach (var subkey in subkeys.EnumerateArray()) {
                var subName = subkey.GetProperty("name").GetString()!;
                table[subName] = BuildNode(subkey);
            }
        }

        return table;
    }

    private static Stream OpenResource() {
        var assembly = typeof(DefinesFactory).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
                               .FirstOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                           ?? throw new InvalidOperationException(
                               $"Embedded resource ending with '{ResourceSuffix}' was not found in {assembly.FullName}.");

        return assembly.GetManifestResourceStream(resourceName)
               ?? throw new InvalidOperationException($"Could not open embedded resource '{resourceName}'.");
    }
}