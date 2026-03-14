using FactorioParanoidal.FactorioMods.Mods;
using FluentAssertions;

namespace FactorioParanoidal.FactorioMods.Tests;

public class FolderFactorioModTests {
    [Fact]
    public async Task FolderFactorioMod_ShouldProperlyLoadModFromFolder() {
        var mod = await FolderFactorioMod.LoadFromDirectory("TestModpack/first-mod");
        mod.Should().NotBeNull();

        mod.Directory.Should().NotBeNullOrWhiteSpace();

        mod.Info.Should().NotBeNull();
        mod.Info.Name.Should().Be("first-mod");
        mod.Info.Version.Should().Be(new Version(1, 0, 1));
        mod.Info.Title.Should().Be("First mod");
        mod.Info.FactorioVersion.Should().Be(new Version(1, 1));
        mod.Info.Author.Should().Be("SKProCH");
        mod.Info.Contact.Should().Be("contact");
        mod.Info.Homepage.Should().Be("homepage");
        mod.Info.Description.Should().Be("First test mod");
        mod.Info.Dependencies.Should().HaveCount(2);
    }

    
    [Fact]
    public async Task Test() {
        var modpack = await FactorioModpack.LoadFromDirectory("C:\\ParanoidalTest\\mods");
        modpack.SortModsByLoadOrder();

        var loadedMods = modpack.Mods.ToList();
        for (var i = loadedMods.Count - 1; i >= 0; i--) {
            var beforeMods = loadedMods[i..];
            var dependencies = loadedMods[i].Info.Dependencies ?? Enumerable.Empty<Mods.Dependencies.FactorioModDependency>();
            var all = dependencies.Where(dependency => dependency.AffectsSorting && beforeMods.Any(mod => mod.Info.Name == dependency.Name));
            if (all.Count() != 0) {
                Console.Write("");
            }
        }
    }
    
}