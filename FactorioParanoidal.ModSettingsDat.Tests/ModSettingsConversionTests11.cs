using System.Text.Encodings.Web;
using System.Text.Json;
using AwesomeAssertions;
using FactorioParanoidal.Models.PropertyTrees;
using Xunit;

namespace FactorioParanoidal.ModSettingsDat.Tests;

public class ModSettingsConversionTests11 {
    private const string DatFilePath = "TestData/mod-settings-1.1.dat";
    private const string JsonFilePath = "TestData/mod-settings-1.1.json";

    public JsonSerializerOptions SerializerOptions { get; }
        = new() {
            WriteIndented = true, Converters = { new FactorioPropertyTreeJsonConverter() }, NewLine = "\n",
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

    [Fact]
    public void Deserialize_Factorio11_ShouldMatchExpectedJson() {
        // Arrange
        using var datStream = File.OpenRead(DatFilePath);
        var expectedJson = File.ReadAllText(JsonFilePath);

        // Act
        var modSettings = ModSettingsConverter.Deserialize(datStream);
        var actualJson = JsonSerializer.Serialize(modSettings, SerializerOptions);

        // Assert
        actualJson.Should().Be(expectedJson);
    }

    [Fact]
    public void Roundtrip_Factorio11_ShouldPreserveData() {
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
    public void Serialize_Factorio11_ShouldProduceByteIdenticalOutput() {
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