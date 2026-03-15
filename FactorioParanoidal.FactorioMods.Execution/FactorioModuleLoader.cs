using FactorioParanoidal.FactorioMods.Mods;
using Lua;

namespace FactorioParanoidal.FactorioMods.Execution;

public class FactorioModuleLoader : ILuaModuleLoader {
    private readonly Dictionary<string, string> _modPaths;

    public FactorioModuleLoader(IEnumerable<IFactorioMod> mods) {
        _modPaths = new Dictionary<string, string>();
        foreach (var mod in mods) {
            if (mod is FolderFactorioMod folderMod) {
                _modPaths[mod.Info.Name] = folderMod.Directory;
            }
        }
    }

    public bool Exists(string moduleName) {
        var path = ResolvePath(moduleName);
        return path != null && File.Exists(path);
    }

    public async ValueTask<LuaModule> LoadAsync(string moduleName, CancellationToken cancellationToken = default) {
        var path = ResolvePath(moduleName);
        if (path == null) {
            throw new FileNotFoundException($"Could not resolve Lua module: {moduleName}");
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return new LuaModule(content, path);
    }

    private string? ResolvePath(string moduleName) {
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

                if (_modPaths.TryGetValue(modName, out var modDirectory)) {
                    return Path.Combine(modDirectory, subPath);
                }
            }
        }

        // Fallback or relative resolution?
        // Factorio usually requires the __mod-name__ prefix or it's relative to the current file.
        // For simplicity in this loader, we mostly expect __mod-name__.
        return null;
    }
}