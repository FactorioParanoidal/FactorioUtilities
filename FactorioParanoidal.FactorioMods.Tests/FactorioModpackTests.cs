using FluentAssertions;

namespace FactorioParanoidal.FactorioMods.Tests;

public class FactorioModpackTests {
    [Fact]
    public async Task FactorioModpack_ShouldLoadEveryModCorrectly() {
        var modpack = await FactorioModpack.LoadFromDirectory("TestModpack");
        modpack.Should().NotBeNull();

        modpack.Should().HaveCount(2);
        modpack.First().Info.Name.Should().Be("first-mod");
        modpack.Last().Info.Name.Should().Be("second-mod");
    }
}