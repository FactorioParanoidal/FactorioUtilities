using FactorioParanoidal.FactorioMods.Execution.Prototypes;
using FactorioParanoidal.FactorioMods.Mods;
using FluentAssertions;
using Xunit;

namespace FactorioParanoidal.FactorioMods.Execution.Tests;

public class GeneratedPrototypesTests : IDisposable {
    private readonly string _modDir;
    private readonly string _tempDir;

    public GeneratedPrototypesTests() {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _modDir = Path.Combine(_tempDir, "test-mod");
        Directory.CreateDirectory(_modDir);
    }

    public void Dispose() {
        if (Directory.Exists(_tempDir)) {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task RunAllStages_PopulatesAccumulatorPrototype() {
        // Arrange
        var info = new FactorioModInfo
            { Name = "test-mod", Version = new Version(1, 0, 0), Title = "test", Author = "test" };
        var mod = new FolderFactorioMod(info, _modDir);

        var dataLuaPath = Path.Combine(_modDir, "data.lua");
        File.WriteAllText(dataLuaPath, @"
            data:extend({
                {
                    type = 'accumulator',
                    name = 'test-accumulator',
                    max_health = 150,
                    energy_source =
                    {
                        type = 'electric',
                        buffer_capacity = '5MJ',
                        usage_priority = 'tertiary',
                        input_flow_limit = '300kW',
                        output_flow_limit = '300kW'
                    },
                    circuit_wire_max_distance = 15.5
                }
            })
        ");

        using var engine = new FactorioLuaEngine(new[] { mod });

        // Act
        await engine.RunAllStages();

        // Assert
        engine.Registry.Prototypes.Should().ContainKey("accumulator");
        var prototype = engine.Registry.GetPrototype("accumulator", "test-accumulator");
        prototype.Should().BeOfType<AccumulatorPrototype>();

        var accumulator = (AccumulatorPrototype)prototype!;
        accumulator.Name.Should().Be("test-accumulator");
        accumulator.Type.Should().Be("accumulator");
        accumulator.CircuitWireMaxDistance.Should().Be(15.5);

        // Complex properties are currently object-mapped or string-mapped in my simple LuaValueUtility
        // Let's check if EnergySource (ElectricEnergySource) was handled (it's object currently in my generator logic for complex types)
        accumulator.EnergySource.Should().NotBeNull();
    }
}