using System.Collections.Immutable;
using FactorioParanoidal.FactorioMods.Execution.Proxies;
using FactorioParanoidal.FactorioMods.Mods;
using Lua;
using Lua.Standard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FactorioParanoidal.FactorioMods.Execution;

public class FactorioLuaEngine : IDisposable {
    private readonly FactorioModuleLoader _loader;
    private readonly ILogger<FactorioLuaEngine> _logger;
    private readonly ImmutableArray<IFactorioMod> _mods;
    private readonly PrototypeRegistry _registry;
    private readonly LuaState _state;

    public FactorioLuaEngine(IEnumerable<IFactorioMod> mods, ILoggerFactory? loggerFactory = null) {
        loggerFactory ??= NullLoggerFactory.Instance;
        _logger = loggerFactory.CreateLogger<FactorioLuaEngine>();
        _mods = [..mods];
        _loader = new FactorioModuleLoader(_mods, loggerFactory.CreateLogger<FactorioModuleLoader>());
        _registry = new PrototypeRegistry();

        _logger.LogDebug("Initializing Factorio Lua engine with {ModCount} mods", _mods.Length);

        _state = LuaState.Create();
        _state.OpenStandardLibraries();
        _state.ModuleLoader = _loader;

        SetupEnvironment();
        _logger.LogDebug("Factorio Lua engine initialized");
    }

    public PrototypeRegistry Registry => _registry;

    public void Dispose() {
        _logger.LogDebug("Disposing Factorio Lua engine");
        _state.Dispose();
    }

    private void SetupEnvironment() {
        // Setup 'package.searchers' for resolving relative mod file requires
        var packageTable = _state.Environment[(LuaValue)"package"].Read<LuaTable>();
        var packageSearchers = packageTable[(LuaValue)"searchers"].Read<LuaTable>();
        packageSearchers[3] = new LuaFunction("resolve_relative_mod_file_path", _loader.ResolveRelativeModFilePathLua);

        // Replace require with a sentinel-aware version.
        // LuaCSharp runs the loader with 0 args so `...` inside a module chunk is nil, not the
        // module name. flib's data-util.lua does `if ... ~= "__flib__.data-util" then return
        // require("__flib__.data-util") end` — without a sentinel this recurses infinitely.
        // Standard Lua 5.4 sets package.loaded[name] = true before running the loader.
        _state.Environment["require"] = new LuaFunction("require", async (context, ct) => {
            var name = context.GetArgument<string>(0);
            var loaded = _state.LoadedModules;

            if (loaded.TryGetValue(name, out var cached) && cached != LuaValue.Nil)
                return context.Return(cached);

            _logger.LogDebug("Requiring Lua module {ModuleName}", name);

            // Sentinel: breaks re-entrant require() for the same module
            loaded[name] = new LuaValue(true);

            LuaFunction loader;
            if (_loader.Exists(name)) {
                var module = await _loader.LoadAsync(name, ct);
                loader = context.State.Load(module.ReadText(), module.Name);
            }
            else {
                loader = await FindLoaderViaSearchers(context.State, name, ct);
            }

            await context.State.RunAsync(loader, 0, context.ReturnFrameBase, ct);
            var result = context.State.Stack[context.ReturnFrameBase];
            loaded[name] = result != LuaValue.Nil ? result : new LuaValue(true);

            return context.Return(loaded[name]);
        });

        // Setup 'data' table
        var dataTable = new LuaTable();
        dataTable["raw"] = DataRawProxy.Create(_state, _registry);

        // data:extend(table)
        dataTable["extend"] =
            new LuaFunction(async (context, _) => {
                var tableArgIndex = context.ArgumentCount > 1 ? 1 : 0;
                if (context.ArgumentCount > tableArgIndex &&
                    context.GetArgument(tableArgIndex).TryRead<LuaTable>(out var t)) {
                    _registry.Extend(t);
                }

                return context.Return();
            });

        _state.Environment["data"] = dataTable;

        // 'defines' is an engine-injected global table available in every stage (data, settings, ...).
        // Mods read and extend it (e.g. defines.direction, defines.inventory), so it must exist up front.
        _state.Environment["defines"] = DefinesFactory.Create();

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
            new LuaFunction(async (context, _) => {
                if (context.ArgumentCount > 0) {
                    _logger.LogInformation("Lua: {Message}", context.GetArgument(0));
                }

                return context.Return();
            });

        _state.Environment["table_size"] =
            new LuaFunction(async (context, _) => {
                if (context.ArgumentCount > 0 && context.GetArgument(0).TryRead<LuaTable>(out var t)) {
                    return context.Return(t.ArrayLength + t.HashMapCount); // Approximate table size
                }

                return context.Return(0);
            });

        // Serpent is usually required. For now we might want to provide a dummy or a real one.
        // If we want a real one, we should probably load it from a string or file.
        _state.Environment["serpent"] = CreateSerpentMock();
    }

    // Mirrors ModuleLibrary.FindLoader (internal) — iterates package.searchers
    private static async ValueTask<LuaFunction> FindLoaderViaSearchers(LuaState state, string name,
        CancellationToken ct) {
        var searchers = state.Environment[(LuaValue)"package"].Read<LuaTable>()[(LuaValue)"searchers"].Read<LuaTable>();
        for (var i = 0; i < searchers.GetArraySpan().Length; i++) {
            var searcher = searchers.GetArraySpan()[i];
            if (searcher.Type == LuaValueType.Nil) continue;
            var top = state.Stack.Count;
            state.Stack.Push(searcher);
            state.Stack.Push(name);
            var count = await state.CallAsync(top, top, ct);
            if (count > 0 && state.Stack[top].Type == LuaValueType.Function) {
                var fn = state.Stack[top].Read<LuaFunction>();
                state.Stack.PopUntil(top);
                return fn;
            }

            state.Stack.PopUntil(top);
        }

        throw new FileNotFoundException($"Module '{name}' not found");
    }

    private LuaTable CreateSerpentMock() {
        var serpent = new LuaTable();
        serpent["dump"] =
            new LuaFunction(async (context, _) => {
                if (context.ArgumentCount > 0) return context.Return(context.GetArgument(0).ToString());
                return context.Return("");
            });
        serpent["line"] =
            new LuaFunction(async (context, _) => {
                if (context.ArgumentCount > 0) return context.Return(context.GetArgument(0).ToString());
                return context.Return("");
            });
        serpent["block"] =
            new LuaFunction(async (context, _) => {
                if (context.ArgumentCount > 0) return context.Return(context.GetArgument(0).ToString());
                return context.Return("");
            });
        return serpent;
    }

    public async Task ExecuteModDataPhase(IFactorioMod mod, string fileName) {
        await ExecuteModDataPhaseCore(mod, fileName);
        _registry.RefreshPrototypes();
    }

    private async Task ExecuteModDataPhaseCore(IFactorioMod mod, string fileName) {
        if (mod.FileExists(fileName)) {
            _logger.LogDebug("Executing {ModName}/{FileName}", mod.Info.Name, fileName);
            var content = await mod.ReadFileTextAsync(fileName);
            var virtualPath = $"__{mod.Info.Name}__/{fileName}";
            await _state.DoStringAsync(content, virtualPath);
        }
    }

    public async Task RunAllStages(Action<string, string, Exception>? onError = null) {
        async Task Run(IFactorioMod mod, string file) {
            try { await ExecuteModDataPhaseCore(mod, file); }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to execute {ModName}/{FileName}", mod.Info.Name, file);
                if (onError == null) throw;
                onError(mod.Info.Name, file, ex);
            }
        }

        async Task RunStage(string stage, string file) {
            _logger.LogDebug("Starting Factorio {Stage} stage", stage);
            foreach (var mod in _mods) await Run(mod, file);
            _registry.RefreshPrototypes();
            _logger.LogDebug("Completed Factorio {Stage} stage", stage);
        }

        await RunStage("settings", "settings.lua");
        await RunStage("settings updates", "settings-updates.lua");
        await RunStage("settings final fixes", "settings-final-fixes.lua");
        await RunStage("data", "data.lua");
        await RunStage("data updates", "data-updates.lua");
        await RunStage("data final fixes", "data-final-fixes.lua");
    }
}