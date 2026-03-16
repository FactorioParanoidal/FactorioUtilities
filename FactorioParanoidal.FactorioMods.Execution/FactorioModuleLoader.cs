using FactorioParanoidal.FactorioMods.Execution.Models;
using FactorioParanoidal.FactorioMods.Mods;
using Lua;
using Lua.Runtime;

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
            Console.WriteLine($"[ModuleLoader] Failed to resolve: {moduleName}");
            throw new FileNotFoundException($"Could not resolve Lua module: {moduleName}");
        }

        var (mod, subPath) = resolved.Value;
        var content = await mod.ReadFileTextAsync(subPath, cancellationToken);
        var virtualPath = $"__{mod.Info.Name}__/{subPath}";
        return new LuaModule(virtualPath, content);
    }

    private (IFactorioMod Mod, string SubPath)? Resolve(string moduleName) {
        var modFileReference = ModFileReference.FromRequire(moduleName);
        if (modFileReference.Mod is not null && _mods.TryGetValue(modFileReference.Mod, out var mod)) {
            return (mod, modFileReference.Path);
        }

        // Fallback or relative resolution?
        // Factorio usually requires the __mod-name__ prefix, or it's relative to the current file.
        // We don't have enough info for "relative to the current file" here, we mostly handle __mod-name__ cases.
        // Relative paths handled by ResolveRelativeModFilePathLua
        return null;
    }

    public async ValueTask<int> ResolveRelativeModFilePathLua(
        LuaFunctionExecutionContext context,
        CancellationToken cancellationToken) {
        var relativePath = string.Empty;

        var callStackFrames = context.State.GetCallStackFrames();
        for (var index = callStackFrames.Length - 1; index >= 0; index--) {
            var callStackFrame = callStackFrames[index];
            if (callStackFrame.Function is LuaClosure closure) {
                var modFileReference = ModFileReference.FromRequire(closure.Name);
                relativePath = Path.Combine(modFileReference.Folder, relativePath);
                if (modFileReference.Mod is null) {
                    continue;
                }

                var moduleName = context.GetArgument<string>(0);
                var moduleReference = ModFileReference.FromRequire(moduleName);

                var mod = _mods[modFileReference.Mod];
                var path = Path.Combine(relativePath, moduleReference.Path);
                if (!mod.FileExists(path)) {
                    throw new FileNotFoundException($"Could not resolve relative module path: {path}");
                }

                var luaFileText = await mod.ReadFileTextAsync(path, cancellationToken);
                return context.Return((LuaValue)(LuaFunction)context.State.Load(luaFileText, path));
            }
        }

        return context.Return(LuaValue.Nil);
    }
}