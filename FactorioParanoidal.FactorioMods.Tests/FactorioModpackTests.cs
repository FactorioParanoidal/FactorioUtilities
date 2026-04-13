using AwesomeAssertions;
using FactorioParanoidal.FactorioMods.Mods;
using FactorioParanoidal.FactorioMods.Mods.Dependencies;

namespace FactorioParanoidal.FactorioMods.Tests;

public class FactorioModpackTests {
    [Fact]
    public void SortModsByLoadOrder_ShouldHandleDependenciesCorrectly() {
        // Arrange
        var core = new MockMod("core");
        var baseMod = new MockMod("base", ("core", false, false));
        var modA = new MockMod("mod-a", ("base", false, false));
        var modB = new MockMod("mod-b", ("mod-a", false, false));
        var modC = new MockMod("mod-c", ("base", false, false));

        var allMods = new[] {
            new FactorioModpack.CanBeDisabledMod(core, true),
            new FactorioModpack.CanBeDisabledMod(baseMod, true),
            new FactorioModpack.CanBeDisabledMod(modA, true),
            new FactorioModpack.CanBeDisabledMod(modB, true),
            new FactorioModpack.CanBeDisabledMod(modC, true)
        };

        var modpack = new FactorioModpack(allMods);

        // Act
        modpack.SortModsByLoadOrder();

        // Assert
        var names = modpack.Mods.Select(m => m.Info.Name).ToList();
        names.IndexOf("core").Should().BeLessThan(names.IndexOf("base"));
        names.IndexOf("base").Should().BeLessThan(names.IndexOf("mod-a"));
        names.IndexOf("base").Should().BeLessThan(names.IndexOf("mod-c"));
        names.IndexOf("mod-a").Should().BeLessThan(names.IndexOf("mod-b"));

        // mod-a and mod-c are on the same tier, so they should be sorted alphabetically
        names.IndexOf("mod-a").Should().BeLessThan(names.IndexOf("mod-c"));
    }

    [Fact]
    public void SortModsByLoadOrder_ShouldHandleImplicitDependencies() {
        // Arrange
        var core = new MockMod("core");
        var baseMod = new MockMod("base"); // No explicit deps, but we add core implicitly
        var beltViz = new MockMod("belt-visualizer");
        beltViz.Info.Dependencies = new List<FactorioModDependency>(); // Explicit []
        var ruins = new MockMod("AbandonedRuins");
        ruins.Info.Dependencies = null; // Missing field

        var allMods = new[] {
            new FactorioModpack.CanBeDisabledMod(core, true),
            new FactorioModpack.CanBeDisabledMod(baseMod, true),
            new FactorioModpack.CanBeDisabledMod(beltViz, true),
            new FactorioModpack.CanBeDisabledMod(ruins, true)
        };

        var modpack = new FactorioModpack(allMods);

        // Act
        modpack.SortModsByLoadOrder();

        // Assert
        var names = modpack.Mods.Select(m => m.Info.Name).ToList();

        // core is Tier 0
        // base is Tier 1 (depends on core)
        // belt-visualizer is Tier 1 (depends on core)
        // AbandonedRuins is Tier 2 (depends on base)

        names.IndexOf("core").Should().BeLessThan(names.IndexOf("base"));
        names.IndexOf("core").Should().BeLessThan(names.IndexOf("belt-visualizer"));
        names.IndexOf("base").Should().BeLessThan(names.IndexOf("AbandonedRuins"));

        // base (ba...) vs belt-visualizer (be...) in Tier 1
        names.IndexOf("base").Should().BeLessThan(names.IndexOf("belt-visualizer"));
    }

    private class MockMod : IFactorioMod {
        public MockMod(string name, params (string Name, bool IsOptional, bool IsIncompatible)[] dependencies) {
            Info = new FactorioModInfo {
                Name = name,
                Title = name,
                Version = new Version(1, 0, 0),
                Author = "Test",
                Dependencies = dependencies.Select(d => new FactorioModDependency {
                    Name = d.Name,
                    Type = d.IsIncompatible ? FactorioModDependencyType.Incompatibility :
                        d.IsOptional ? FactorioModDependencyType.Optional :
                        FactorioModDependencyType.HardRequirement
                }).ToList()
            };
        }

        public FactorioModInfo Info { get; }

        public bool FileExists(string subPath) => false;

        public Task<string> ReadFileTextAsync(string subPath, CancellationToken cancellationToken = default) =>
            throw new FileNotFoundException();
    }
}