using FactorioParanoidal.FactorioMods.Mods;
using FluentAssertions;
using Xunit;

namespace FactorioParanoidal.FactorioMods.Execution.Tests;

public class FactorioLuaEngineTests : IDisposable {
    private readonly string _modDir;
    private readonly string _tempDir;

    public FactorioLuaEngineTests() {
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
    public async Task RunAllStages_ExecutesDataLuaAndPopulatesRegistry() {
        // Arrange
        var info = new FactorioModInfo
            { Name = "test-mod", Version = new Version(1, 0, 0), Title = "test", Author = "test" };
        var mod = new FolderFactorioMod(info, _modDir);

        var dataLuaPath = Path.Combine(_modDir, "data.lua");
        File.WriteAllText(dataLuaPath, @"
            data:extend({
                {
                    type = 'item',
                    name = 'test-item',
                    stack_size = 50
                }
            })
        ");

        using var engine = new FactorioLuaEngine(new[] { mod });

        // Act
        await engine.RunAllStages();

        // Assert
        engine.Registry.Prototypes.Should().ContainKey("item");
        engine.Registry.Prototypes["item"].Should().ContainKey("test-item");

        var item = engine.Registry.Prototypes["item"]["test-item"];
        var stackSize = (uint)item.GetType().GetProperty("StackSize")?.GetValue(item)!;
        stackSize.Should().Be(50);
    }

    [Fact]
    public async Task RunAllStages_HandlesMultiStageOverrides() {
        // Arrange
        var info = new FactorioModInfo
            { Name = "test-mod", Version = new Version(1, 0, 0), Title = "test", Author = "test" };
        var mod = new FolderFactorioMod(info, _modDir);

        File.WriteAllText(Path.Combine(_modDir, "data.lua"), @"
            data:extend({{type = 'item', name = 'test-item', value = 1}})
        ");
        File.WriteAllText(Path.Combine(_modDir, "data-updates.lua"), @"
            data.raw['item']['test-item'].value = 2
        ");

        using var engine = new FactorioLuaEngine(new[] { mod });

        // Act
        await engine.RunAllStages();

        // Assert
        var item = engine.Registry.Prototypes["item"]["test-item"];
        item.ExtraFields["value"].ToString().Should().Be("2");
    }
}