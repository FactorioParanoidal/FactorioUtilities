using System.Text.Json;
using FactorioParanoidal.FactorioMods.ModLists;
using FactorioParanoidal.FactorioMods.Mods;
using FactorioParanoidal.FactorioMods.Mods.Dependencies;

namespace FactorioParanoidal.FactorioMods;

public class FactorioModpack {
    public FactorioModpack(IEnumerable<CanBeDisabledMod> enumerableImplementation) {
        AllMods = enumerableImplementation.ToList();
        Mods = AllMods
            .Where(mod => mod.IsEnabled)
            .Select(mod => mod.Mod)
            .ToList();
    }

    public IReadOnlyList<CanBeDisabledMod> AllMods { get; private set; }
    public IReadOnlyList<IFactorioMod> Mods { get; private set; }

    /// <remarks>
    /// Only loading mods from folders are supported by now
    /// </remarks>
    public static async Task<FactorioModpack> LoadFromDirectory(string directory) {
        var modListPath = Path.Combine(directory, "mod-list.json");
        var factorioModList = new FactorioModList();
        if (File.Exists(modListPath)) {
            await using var modListFileStream = File.OpenRead(modListPath);
            factorioModList = (await JsonSerializer.DeserializeAsync<FactorioModList>(modListFileStream))!;
        }

        var directories = Directory.GetDirectories(directory);
        var loadingTasks = directories.Select(FolderFactorioMod.LoadFromDirectory);
        var mods = await Task.WhenAll(loadingTasks);

        var allMods =
            mods.Select(mod => {
                var isEnabled = !factorioModList.Mods.Any(item => item.Name == mod.Info.Name && !item.Enabled);
                return new CanBeDisabledMod(mod, isEnabled);
            });
        return new FactorioModpack(allMods);
    }

    public void SortModsByLoadOrder() {
        var modsDictionary = new Dictionary<string, LoadingOrderInfo>();
        foreach (var mod in AllMods) {
            modsDictionary[mod.Mod.Info.Name] = new LoadingOrderInfo(mod.Mod, true, mod.IsEnabled, mod.IsReal);
        }

        if (!modsDictionary.ContainsKey("core")) {
            var core = new CanBeDisabledMod(new MissingMod("core"), true, false);
            modsDictionary["core"] = new LoadingOrderInfo(core.Mod, true, core.IsEnabled, core.IsReal);
        }

        if (!modsDictionary.ContainsKey("base")) {
            var baseMod = new CanBeDisabledMod(new MissingMod("base"), true, false);
            modsDictionary["base"] = new LoadingOrderInfo(baseMod.Mod, true, baseMod.IsEnabled, baseMod.IsReal);
        }

        var infos = modsDictionary.Values.ToList();


        foreach (var info in infos) {
            UpdateDependenciesInternal(modsDictionary, info, ModOrderType.Default);
        }

        infos.Sort((x, y) => SortAndOrderLambda(x, y, ModOrderType.Default));

        infos.RemoveAll(info => !info.IsEnabled);

        foreach (var info in infos) {
            UpdateDependenciesInternal(modsDictionary, info, ModOrderType.Real);
        }

        infos.Sort((x, y) => SortAndOrderLambda(x, y, ModOrderType.Real));

        Mods = [
            ..infos
                .Select(info => info.Mod)
        ];

        int SortAndOrderLambda(LoadingOrderInfo x, LoadingOrderInfo y, ModOrderType modOrderType) {
            var xOrderingData = x.GetOrderingData(modOrderType);
            var yOrderingData = y.GetOrderingData(modOrderType);

            // First, sort by tier
            if (xOrderingData.Tier != yOrderingData.Tier)
                return xOrderingData.Tier.CompareTo(yOrderingData.Tier);

            // If both tier and ordering are equal, sort by mod name
            return StrNatCmp0(x.Mod.Info.Name, y.Mod.Info.Name, true);
        }
    }

    public static int StrNatCmp0(string a, string b, bool foldCase = false) {
        if (a == null || b == null)
            throw new ArgumentNullException(a == null ? nameof(a) : nameof(b));

        int i = 0, j = 0;

        // Skip leading whitespace
        while (i < a.Length && char.IsWhiteSpace(a[i])) i++;
        while (j < b.Length && char.IsWhiteSpace(b[j])) j++;

        while (i < a.Length && j < b.Length) {
            char ca = a[i];
            char cb = b[j];

            if (char.IsDigit(ca) && char.IsDigit(cb)) {
                if (ca == '0' || cb == '0') {
                    // Case 1: If either sequence starts with '0', compare character-by-character
                    while (i < a.Length && j < b.Length && char.IsDigit(a[i]) && char.IsDigit(b[j])) {
                        if (a[i] != b[j]) return a[i] - b[j];
                        i++;
                        j++;
                    }
                }
                else {
                    // Case 2: If neither starts with '0', compare numeric value (length first, then first difference)
                    int bias = 0;
                    while (i < a.Length && j < b.Length && char.IsDigit(a[i]) && char.IsDigit(b[j])) {
                        if (bias == 0) {
                            if (a[i] < b[j]) bias = -1;
                            else if (a[i] > b[j]) bias = 1;
                        }

                        i++;
                        j++;
                    }

                    if (i < a.Length && char.IsDigit(a[i])) return 1;
                    if (j < b.Length && char.IsDigit(b[j])) return -1;
                    if (bias != 0) return bias;
                }

                // If one string still has digits, it's "longer" and thus larger
                if (i < a.Length && char.IsDigit(a[i])) return 1;
                if (j < b.Length && char.IsDigit(b[j])) return -1;

                // Continue to next character after numeric sequence
                continue;
            }

            if (foldCase) {
                ca = char.ToUpperInvariant(ca);
                cb = char.ToUpperInvariant(cb);
            }

            if (ca != cb) return ca - cb;

            i++;
            j++;
        }

        if (i == a.Length && j == b.Length) return 0;
        return i < a.Length ? 1 : -1;
    }

    private void UpdateDependenciesInternal(IDictionary<string, LoadingOrderInfo> mods, LoadingOrderInfo mod,
        ModOrderType type) {
        var orderingData = mod.GetOrderingData(type);
        if (orderingData.Tier > -1) {
            return;
        }

        orderingData.AllDependencies = 0;
        orderingData.MetDependencies = 0;
        orderingData.Tier = -2;
        var currentTier = -1;

        // Check version compatibility
        // mod.Valid = IsVersionCompatible(mod);

        var dependencies = mod.Mod.Info.Dependencies ?? new List<FactorioModDependency>();

        // Handle implicit dependencies
        if (mod.Mod.Info.Name != "core") {
            // Every mod implicitly depends on core
            if (!dependencies.Any(d => d.Name == "core")) {
                dependencies = dependencies.ToList();
                dependencies.Add(new FactorioModDependency
                    { Name = "core", Type = FactorioModDependencyType.HardRequirement });
            }

            if (mod.Mod.Info.Name != "base" && !mod.Mod.Info.HasExplicitDependencies) {
                // Mods without "dependencies" field implicitly depend on base
                if (!dependencies.Any(d => d.Name == "base")) {
                    if (dependencies is not List<FactorioModDependency>) dependencies = dependencies.ToList();
                    dependencies.Add(new FactorioModDependency
                        { Name = "base", Type = FactorioModDependencyType.HardRequirement });
                }
            }
        }

        foreach (var dependency in dependencies) {
            orderingData.AllDependencies++;
            if (!mods.TryGetValue(dependency.Name, out var dependencyMod)) {
                if (dependency is { IsOptional: false, IsIncompatible: false }) {
                    mod.IsValid = false;
                }

                continue;
            }

            orderingData.MetDependencies++;

            if (dependency.IsIncompatible) {
                if (!dependencyMod.IsValid)
                    continue;

                if (dependencyMod.IsEnabled) {
                    mod.IsValid = false;
                }

                continue;
            }

            if (!dependency.AffectsSorting)
                continue;

            var dependencyOrderingData = dependencyMod.GetOrderingData(type);

            if (dependencyOrderingData.Tier == -2) {
                // Handle circular dependency
                // List<string> circularDependencyChain = GetCircularDependencyChain(mods, type);
                // throw new ModsDependencyException("Circular dependency detected", circularDependencyChain);
                throw new Exception("Circular dependency detected");
            }

            if (dependencyOrderingData.Tier == -1) {
                UpdateDependenciesInternal(mods, dependencyMod, type);
            }

            if (!dependencyMod.IsEnabled || !dependencyMod.IsValid) {
                if (dependency.IsOptional)
                    continue;

                if (!dependency.IsIncompatible) {
                    mod.IsValid = false;
                }

                continue;
            }

            if (!dependency.IsIncompatible && !dependency.IsSatisfied(dependencyMod.Mod.Info.Version)) {
                mod.IsValid = false;
            }

            currentTier = Math.Max(currentTier, dependencyOrderingData.Tier);
        }

        orderingData.Tier = currentTier + 1;
    }

    private class MissingMod(string name) : IFactorioMod {
        public FactorioModInfo Info { get; } = new() {
            Name = name,
            Version = new Version(int.MaxValue, int.MaxValue),
            Title = string.Empty,
            Author = string.Empty
        };

        public bool FileExists(string subPath) => false;

        public Task<string> ReadFileTextAsync(string subPath, CancellationToken cancellationToken = default) =>
            throw new FileNotFoundException($"Mod '{Info.Name}' is missing and cannot provide files.");
    }

    public class CanBeDisabledMod {
        public CanBeDisabledMod(IFactorioMod mod, bool isEnabled, bool isReal = true) {
            Mod = mod;
            IsEnabled = isEnabled;
            IsReal = isReal;
        }

        public IFactorioMod Mod { get; }
        public bool IsEnabled { get; }
        public bool IsReal { get; }
    }

    private class LoadingOrderInfo(IFactorioMod mod, bool isValid, bool isEnabled, bool isReal) {
        private Dictionary<ModOrderType, OrderingData> _orderingDatas = new();
        public IFactorioMod Mod { get; } = mod;
        public bool IsValid { get; set; } = isValid;
        public bool IsEnabled { get; } = isEnabled;
        public bool IsReal { get; } = isReal;

        public OrderingData GetOrderingData(ModOrderType type) {
            return _orderingDatas.GetOrCreate(type, () => new OrderingData());
        }

        public class OrderingData {
            public int Tier { get; set; } = -1;
            public int AllDependencies { get; set; }
            public int MetDependencies { get; set; }
        }
    }

    private enum ModOrderType {
        Default,
        Real
    }
}