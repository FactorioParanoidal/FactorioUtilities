using FactorioParanoidal.FactorioMods.Execution.Proxies;
using FactorioParanoidal.FactorioMods.Mods;
using Lua;
using Lua.Standard;

namespace FactorioParanoidal.FactorioMods.Execution;

public class FactorioLuaEngine : IDisposable {
    private readonly FactorioModuleLoader _loader;
    private readonly IEnumerable<IFactorioMod> _mods;
    private readonly PrototypeRegistry _registry;
    private readonly LuaState _state;

    public FactorioLuaEngine(IEnumerable<IFactorioMod> mods) {
        _mods = mods;
        _loader = new FactorioModuleLoader(mods);
        _registry = new PrototypeRegistry();

        _state = LuaState.Create();
        _state.OpenStandardLibraries();
        _state.ModuleLoader = _loader;

        SetupEnvironment();
    }

    public PrototypeRegistry Registry => _registry;

    public void Dispose() {
        _state.Dispose();
    }

    private void SetupEnvironment() {
        // Setup 'data' table
        var dataTable = new LuaTable();
        dataTable["raw"] = DataRawProxy.Create(_state, _registry);

        // data:extend(table)
        dataTable["extend"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                int tableArgIndex = context.ArgumentCount > 1 ? 1 : 0;
                if (context.ArgumentCount > tableArgIndex &&
                    context.GetArgument(tableArgIndex).TryRead<LuaTable>(out var t)) {
                    _registry.Extend(t);
                }

                return context.Return();
            });

        _state.Environment["data"] = dataTable;

        // Setup 'mods' table (mod-name -> version)
        var modsTable = new LuaTable();
        foreach (var mod in _mods) {
            modsTable[mod.Info.Name] = mod.Info.Version.ToString();
        }

        _state.Environment["mods"] = modsTable;

        // Setup empty 'settings' table (Factorio has settings.startup, settings.runtime_global, settings.runtime_per_user)
        var settingsTable = new LuaTable();
        settingsTable["startup"] = new LuaTable();
        settingsTable["runtime_global"] = new LuaTable();
        settingsTable["runtime_per_user"] = new LuaTable();
        _state.Environment["settings"] = settingsTable;

        // Common Factorio globals
        _state.Environment["log"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount > 0) {
                    Console.WriteLine($"[Lua Log] {context.GetArgument(0)}");
                }

                return context.Return();
            });

        _state.Environment["table_size"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount > 0 && context.GetArgument(0).TryRead<LuaTable>(out var t)) {
                    return context.Return(t.ArrayLength + t.HashMapCount); // Approximate table size
                }

                return context.Return(0);
            });

        // Serpent is usually required. For now we might want to provide a dummy or a real one.
        // If we want a real one, we should probably load it from a string or file.
        _state.Environment["serpent"] = CreateSerpentMock();
    }

    private LuaTable CreateSerpentMock() {
        var serpent = new LuaTable();
        serpent["dump"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount > 0) return context.Return(context.GetArgument(0).ToString());
                return context.Return("");
            });
        serpent["line"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount > 0) return context.Return(context.GetArgument(0).ToString());
                return context.Return("");
            });
        serpent["block"] =
            new LuaFunction(async (LuaFunctionExecutionContext context, CancellationToken cancellationToken) => {
                if (context.ArgumentCount > 0) return context.Return(context.GetArgument(0).ToString());
                return context.Return("");
            });
        return serpent;
    }

    public async Task ExecuteModDataPhase(IFactorioMod mod, string fileName) {
        if (mod is FolderFactorioMod folderMod) {
            string filePath = Path.Combine(folderMod.Directory, fileName);
            if (File.Exists(filePath)) {
                var content = await File.ReadAllTextAsync(filePath);
                await _state.DoStringAsync(content, filePath);
            }
        }
    }

    public async Task RunAllStages() {
        // 1. Settings
        foreach (var mod in _mods) await ExecuteModDataPhase(mod, "settings.lua");
        foreach (var mod in _mods) await ExecuteModDataPhase(mod, "settings-updates.lua");
        foreach (var mod in _mods) await ExecuteModDataPhase(mod, "settings-final-fixes.lua");

        // 2. Data
        foreach (var mod in _mods) await ExecuteModDataPhase(mod, "data.lua");

        // 3. Data Updates
        foreach (var mod in _mods) await ExecuteModDataPhase(mod, "data-updates.lua");

        // 4. Data Final Fixes
        foreach (var mod in _mods) await ExecuteModDataPhase(mod, "data-final-fixes.lua");
    }
}