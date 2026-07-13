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
        return new LuaModule(moduleName, content);
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

        // Find the innermost call-stack frame that belongs to a known mod — that is the file
        // that issued the require (walking from top of stack, i.e. the most recent frame first).
        var callStackFrames = context.State.GetCallStackFrames();
        for (var index = callStackFrames.Length - 1; index >= 0; index--) {
            if (callStackFrames[index].Function is not LuaClosure closure) continue;

            var currentFile = ModFileReference.FromRequire(closure.Name);
            if (currentFile.Mod is null || !_mods.TryGetValue(currentFile.Mod, out var currentMod)) continue;

            // Resolution order (mirrors Factorio):
            //   1. mod-root relative  e.g. require("folder.file") → folder/file.lua
            //   2. same-directory relative  e.g. require("sibling") from a/b.lua → a/sibling.lua
            //   3. core/lualib  e.g. require("util") → core/lualib/util.lua
            string[] paths = [
                Normalize(moduleReference.Path),
                Normalize(Path.Combine(currentFile.Folder, moduleReference.Path))
            ];
            foreach (var path in paths) {
                if (!currentMod.FileExists(path)) continue;
                var text = await currentMod.ReadFileTextAsync(path, cancellationToken);
                return context.Return(
                    (LuaValue)(LuaFunction)context.State.Load(text, $"__{currentMod.Info.Name}__/{path}"));
            }

            if (_mods.TryGetValue("core", out var core)) {
                var libPath = Normalize(Path.Combine("lualib", moduleReference.Path));
                if (core.FileExists(libPath)) {
                    var text = await core.ReadFileTextAsync(libPath, cancellationToken);
                    return context.Return((LuaValue)(LuaFunction)context.State.Load(text, $"__core__/{libPath}"));
                }
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