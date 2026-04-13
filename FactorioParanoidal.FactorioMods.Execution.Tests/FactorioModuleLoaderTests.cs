using AwesomeAssertions;
using FactorioParanoidal.FactorioMods.Execution.Tests.Helpers;
using FactorioParanoidal.FactorioMods.Mods;
using Xunit;

namespace FactorioParanoidal.FactorioMods.Execution.Tests;

public class FactorioModuleLoaderTests {
    [Fact]
    public void ResolvePath_WithModPrefix_ResolvesCorrectly() {
        // Arrange
        var info = new FactorioModInfo
            { Name = "base", Version = new Version(1, 0, 0), Title = "base", Author = "Wube" };
        var baseMod = new InMemoryFactorioMod(info);
        baseMod.AddFile("prototypes/entity.lua", "return {}");

        var loader = new FactorioModuleLoader(new[] { baseMod });

        // Act & Assert
        loader.Exists("__base__.prototypes.entity").Should().BeTrue();
        loader.Exists("__base__/prototypes/entity.lua").Should().BeTrue();
    }
}