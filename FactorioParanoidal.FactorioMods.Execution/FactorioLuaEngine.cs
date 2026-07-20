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
    private readonly Dictionary<FactorioDataStage, PrototypeRegistry> _stageRegistries = [];
    private PrototypeRegistry _activeRegistry;
    private LuaState _state = null!;

    public FactorioLuaEngine(IEnumerable<IFactorioMod> mods, ILoggerFactory? loggerFactory = null) {
        loggerFactory ??= NullLoggerFactory.Instance;
        _logger = loggerFactory.CreateLogger<FactorioLuaEngine>();
        _mods = [..mods];
        _loader = new FactorioModuleLoader(_mods, loggerFactory.CreateLogger<FactorioModuleLoader>());
        _registry = new PrototypeRegistry();
        _activeRegistry = _registry;

        _logger.LogDebug("Initializing Factorio Lua engine with {ModCount} mods", _mods.Length);

        ResetState(_registry);
        _logger.LogDebug("Factorio Lua engine initialized");
    }

    public PrototypeRegistry Registry => _registry;

    public PrototypeRegistry? SettingsRegistry =>
        _stageRegistries.GetValueOrDefault(FactorioDataStage.SettingsFinalFixes);

    public IReadOnlyDictionary<FactorioDataStage, PrototypeRegistry> StageRegistries => _stageRegistries;

    public void Dispose() {
        _logger.LogDebug("Disposing Factorio Lua engine");
        _state.Dispose();
    }

    private void ResetState(PrototypeRegistry registry) {
        _state?.Dispose();
        _activeRegistry = registry;
        _state = LuaState.Create();
        _state.OpenStandardLibraries();
        _state.ModuleLoader = _loader;
        SetupEnvironment();
    }

    private void SetupEnvironment() {
        // Setup 'package.searchers' for resolving relative mod file requires
        var packageTable = _state.Environment[(LuaValue)"package"].Read<LuaTable>();
        var packageSearchers = packageTable[(LuaValue)"searchers"].Read<LuaTable>();
        packageSearchers[2] = new LuaFunction("resolve_relative_mod_file_path", _loader.ResolveRelativeModFilePathLua);
        packageSearchers[3] = LuaValue.Nil;
        packageTable["searchpath"] = LuaValue.Nil;

        // Replace require with a sentinel-aware version.
        // LuaCSharp runs the loader with 0 args so `...` inside a module chunk is nil, not the
        // module name. flib's data-util.lua does `if ... ~= "__flib__.data-util" then return
        // require("__flib__.data-util") end` — without a sentinel this recurses infinitely.
        // Standard Lua 5.4 sets package.loaded[name] = true before running the loader.
        _state.Environment["require"] = new LuaFunction("require", async (context, ct) => {
            var name = context.GetArgument<string>(0);
            var loaded = _state.LoadedModules;
            var cacheKey = _loader.GetCacheKey(context.State, name);

            if (loaded.TryGetValue(cacheKey, out var cached) && cached != LuaValue.Nil)
                return context.Return(cached);

            _logger.LogDebug("Requiring Lua module {ModuleName}", name);

            // Sentinel: breaks re-entrant require() for the same module
            loaded[cacheKey] = new LuaValue(true);

            LuaFunction loader;
            if (_loader.Exists(name)) {
                var module = await _loader.LoadAsync(name, ct);
                loader = context.State.Load(module.ReadText(), module.Name);
            }
            else {
                loader = await FindLoaderViaSearchers(context.State, name, ct);
            }

            // The loader is executed as a regular Lua call. Its return values are
            // written to the call frame, not necessarily to ReturnFrameBase.
            var returnBase = context.State.Stack.Count;
            await context.State.RunAsync(loader, 0, returnBase, ct);
            var result = context.State.Stack[returnBase];
            loaded[cacheKey] = result != LuaValue.Nil ? result : new LuaValue(true);

            return context.Return(loaded[cacheKey]);
        });

        // Setup 'data' table
        var dataTable = new LuaTable();
        dataTable["raw"] = DataRawProxy.Create(_state, _activeRegistry);

        // data:extend(table)
        dataTable["extend"] =
            new LuaFunction(async (context, _) => {
                var tableArgIndex = context.ArgumentCount > 1 ? 1 : 0;
                if (context.ArgumentCount > tableArgIndex &&
                    context.GetArgument(tableArgIndex).TryRead<LuaTable>(out var t)) {
                    _activeRegistry.Extend(t);
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

        FactorioAuxiliaryLibraries.Install(_state, _logger);
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

    public async Task ExecuteModDataPhase(IFactorioMod mod, string fileName) {
        await ExecuteModDataPhaseCore(mod, fileName);
        _activeRegistry.RefreshPrototypes();
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

        async Task RunStage(FactorioDataStage stage, string file) {
            _logger.LogDebug("Starting Factorio {Stage} stage", stage);
            foreach (var mod in _mods) await Run(mod, file);
            _activeRegistry.RefreshPrototypes();
            _stageRegistries[stage] = _activeRegistry.CreateSnapshot();
            _logger.LogDebug("Completed Factorio {Stage} stage", stage);
        }

        _stageRegistries.Clear();
        var settingsRegistry = new PrototypeRegistry();
        ResetState(settingsRegistry);
        await RunStage(FactorioDataStage.Settings, "settings.lua");
        await RunStage(FactorioDataStage.SettingsUpdates, "settings-updates.lua");
        await RunStage(FactorioDataStage.SettingsFinalFixes, "settings-final-fixes.lua");
        var startupSettings = ReadStartupSettings(settingsRegistry);

        // Factorio creates a fresh shared state for the prototype stage.
        ResetState(_registry);
        PopulateSettings(startupSettings);
        await RunStage(FactorioDataStage.Data, "data.lua");
        await RunStage(FactorioDataStage.DataUpdates, "data-updates.lua");
        await RunStage(FactorioDataStage.DataFinalFixes, "data-final-fixes.lua");
    }

    private static Dictionary<string, LuaValue> ReadStartupSettings(PrototypeRegistry registry) {
        var settings = new Dictionary<string, LuaValue>();
        foreach (var prototypeType in new[] { "bool-setting", "int-setting", "double-setting", "string-setting" }) {
            if (!registry.Prototypes.TryGetValue(prototypeType, out var prototypes)) continue;

            foreach (var name in prototypes.Keys) {
                var raw = registry.GetRawTable(prototypeType, name)!;
                if (raw["setting_type"].TryRead<string>(out var settingType) && settingType == "startup") {
                    settings[name] = raw["default_value"];
                }
            }
        }

        return settings;
    }

    private void PopulateSettings(IReadOnlyDictionary<string, LuaValue> startupSettings) {
        var settings = _state.Environment[(LuaValue)"settings"].Read<LuaTable>();
        var startup = settings[(LuaValue)"startup"].Read<LuaTable>();
        foreach (var (name, defaultValue) in startupSettings) {
            var value = new LuaTable();
            value["value"] = defaultValue;
            startup[name] = value;
        }
    }
}