using System.Text.Json;
using AwesomeAssertions;
using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioMods.Tests;

public class FactorioVersionTests {
    [Fact]
    public void ModInfoVersion_ShouldPreserveOriginalFormatting() {
        const string json = """
                            {
                              "name": "test",
                              "version": "2.0.08",
                              "title": "Test",
                              "author": "Test"
                            }
                            """;

        var info = JsonSerializer.Deserialize<FactorioModInfo>(json)!;

        info.Version.ToString().Should().Be("2.0.08");
        ((Version)info.Version).Should().Be(new Version(2, 0, 8));
        JsonSerializer.Serialize(info).Should().Contain("\"version\":\"2.0.08\"");
    }

    [Fact]
    public void SystemVersion_ShouldImplicitlyConvertToFactorioVersion() {
        var info = new FactorioModInfo {
            Name = "test",
            Version = new Version(2, 0, 8),
            Title = "Test",
            Author = "Test"
        };

        info.Version.ToString().Should().Be("2.0.8");
    }
}