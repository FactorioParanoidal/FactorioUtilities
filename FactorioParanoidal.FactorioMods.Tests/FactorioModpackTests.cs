using FluentAssertions;

namespace FactorioParanoidal.FactorioMods.Tests;

public class FactorioModpackTests {
    [Fact]
    public async Task FactorioModpack_ShouldLoadEveryModCorrectly() {
        var modpack = await FactorioModpack.LoadFromDirectory("TestModpack");
        modpack.Should().NotBeNull();

        modpack.Should().HaveCount(2);
        modpack.Should().ContainSingle(mod => mod.Info.Name == "first-mod");
        modpack.Should().ContainSingle(mod => mod.Info.Name == "second-mod");
    }
}