using FactorioParanoidal.FactorioMods.Mods;
using Lua;

namespace FactorioParanoidal.FactorioMods.Execution;

public class FactorioModuleLoader : ILuaModuleLoader {
    private readonly Dictionary<string, IFactorioMod> _mods;

    public FactorioModuleLoader(IEnumerable<IFactorioMod> mods) {
        _mods = mods.ToDictionary(m => m.Info.Name);
    }

    public bool Exists(string moduleName) {
        var resolved = Resolve(moduleName);
        return resolved != null && resolved.Value.Mod.FileExists(resolved.Value.SubPath);
    }

    public async ValueTask<LuaModule> LoadAsync(string moduleName, CancellationToken cancellationToken = default) {
        var resolved = Resolve(moduleName);
        if (resolved == null) {
            throw new FileNotFoundException($"Could not resolve Lua module: {moduleName}");
        }

        var (mod, subPath) = resolved.Value;
        var content = await mod.ReadFileTextAsync(subPath, cancellationToken);
        var virtualPath = $"__{mod.Info.Name}__/{subPath}";
        return new LuaModule(content, virtualPath);
    }

    private (IFactorioMod Mod, string SubPath)? Resolve(string moduleName) {
        // Factorio uses dots or slashes, and __mod-name__ prefix
        // Example: require("__base__.prototypes.entity.demo-entities")
        // Example: require("__base__/prototypes/entity/demo-entities.lua")

        string path;
        if (moduleName.EndsWith(".lua")) {
            path = moduleName;
        }
        else {
            path = moduleName.Replace('.', '/') + ".lua";
        }

        if (path.StartsWith("__")) {
            var secondDoubleUnderscore = path.IndexOf("__", 2);
            if (secondDoubleUnderscore != -1) {
                var modName = path.Substring(2, secondDoubleUnderscore - 2);
                var subPath = path.Substring(secondDoubleUnderscore + 2).TrimStart('/');

                if (_mods.TryGetValue(modName, out var mod)) {
                    return (mod, subPath);
                }
            }
        }

        // Fallback or relative resolution?
        // Factorio usually requires the __mod-name__ prefix or it's relative to the current file.
        // For simplicity in this loader, we mostly expect __mod-name__.
        return null;
    }
}