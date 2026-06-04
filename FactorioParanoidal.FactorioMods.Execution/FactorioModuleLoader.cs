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
        var moduleReference = ModFileReference.FromRequire(context.GetArgument<string>(0));

        // Walk the call stack to find the file (and therefore the mod) that issued the require.
        var callStackFrames = context.State.GetCallStackFrames();
        for (var index = callStackFrames.Length - 1; index >= 0; index--) {
            if (callStackFrames[index].Function is not LuaClosure closure) {
                continue;
            }

            var currentFile = ModFileReference.FromRequire(closure.Name);
            if (currentFile.Mod is null || !_mods.TryGetValue(currentFile.Mod, out var currentMod)) {
                // Frame without a known owning mod (e.g. a standard-library frame); keep looking.
                continue;
            }

            // Factorio resolves a bare require by trying, in order:
            //   1. relative to the current mod's root (the common "folder.file" style)
            //   2. relative to the requiring file's own directory (sibling files)
            //   3. core/lualib (shared helpers such as require "util")
            var candidates = new List<(IFactorioMod Mod, string Path)> {
                (currentMod, Normalize(moduleReference.Path)),
                (currentMod, Normalize(Path.Combine(currentFile.Folder, moduleReference.Path)))
            };
            if (_mods.TryGetValue("core", out var core)) {
                candidates.Add((core, Normalize(Path.Combine("lualib", moduleReference.Path))));
            }

            foreach (var (mod, path) in candidates) {
                if (!mod.FileExists(path)) {
                    continue;
                }

                var luaFileText = await mod.ReadFileTextAsync(path, cancellationToken);
                // Name the chunk with the __mod__ prefix so requires it issues resolve against the right mod.
                var virtualPath = $"__{mod.Info.Name}__/{path}";
                return context.Return((LuaValue)(LuaFunction)context.State.Load(luaFileText, virtualPath));
            }

            throw new FileNotFoundException(
                $"Could not resolve relative module '{moduleReference.Path}' required from {closure.Name}");
        }

        return context.Return(LuaValue.Nil);
    }

    private static string Normalize(string path) {
        return path.Replace('\\', '/');
    }
}