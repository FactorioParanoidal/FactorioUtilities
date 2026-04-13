using AwesomeAssertions;
using Xunit;

namespace FactorioParanoidal.ModSettingsDat.Tests;

public class ModSettingsConversionTests20 {
    private const string DatFilePath = "TestData/mod-settings-2.0.dat";

    [Fact]
    public void Deserialize_Factorio20_ShouldNotThrow() {
        // Arrange
        using var datStream = File.OpenRead(DatFilePath);

        // Act
        var modSettings = ModSettingsConverter.Deserialize(datStream);

        // Assert
        modSettings.Should().NotBeNull();
        modSettings.Version.Major.Should().Be(2);
        modSettings.Content.Startup.Should().NotBeEmpty();
    }

    [Fact]
    public void Roundtrip_Factorio20_ShouldPreserveData() {
        // Arrange
        using var originalDatStream = File.OpenRead(DatFilePath);
        var originalSettings = ModSettingsConverter.Deserialize(originalDatStream);

        // Act
        using var ms = new MemoryStream();
        ModSettingsConverter.Serialize(originalSettings, ms);
        ms.Position = 0;

        var roundtripSettings = ModSettingsConverter.Deserialize(ms);

        // Assert
        roundtripSettings.Version.Should().Be(originalSettings.Version);
        roundtripSettings.Content.Startup.Should().BeEquivalentTo(originalSettings.Content.Startup);
        roundtripSettings.Content.RuntimeGlobal.Should().BeEquivalentTo(originalSettings.Content.RuntimeGlobal);
        roundtripSettings.Content.RuntimePerUser.Should().BeEquivalentTo(originalSettings.Content.RuntimePerUser);
    }

    [Fact]
    public void Serialize_Factorio20_ShouldProduceByteIdenticalOutput() {
        // Arrange
        var originalBytes = File.ReadAllBytes(DatFilePath);
        using var originalDatStream = new MemoryStream(originalBytes);
        var modSettings = ModSettingsConverter.Deserialize(originalDatStream);

        // Act
        using var ms = new MemoryStream();
        ModSettingsConverter.Serialize(modSettings, ms);
        var resultBytes = ms.ToArray();

        // Assert
        resultBytes.Should().Equal(originalBytes);
    }
}