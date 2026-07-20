using AwesomeAssertions;
using FactorioParanoidal.FactorioMods.Execution.Tests.Helpers;
using FactorioParanoidal.FactorioMods.Mods;
using Xunit;

namespace FactorioParanoidal.FactorioMods.Execution.Tests;

public class FactorioLuaEngineTests {
    [Fact]
    public async Task RunAllStages_ExecutesDataLuaAndPopulatesRegistry() {
        // Arrange
        var info = new FactorioModInfo
            { Name = "test-mod", Version = new Version(1, 0, 0), Title = "test", Author = "test" };
        var mod = new InMemoryFactorioMod(info);

        mod.AddFile("data.lua", @"
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
        var mod = new InMemoryFactorioMod(info);

        mod.AddFile("data.lua", @"
            data:extend({{type = 'item', name = 'test-item', value = 1}})
        ");
        mod.AddFile("data-updates.lua", @"
            data.raw['item']['test-item'].value = 2
        ");

        using var engine = new FactorioLuaEngine(new[] { mod });

        // Act
        await engine.RunAllStages();

        // Assert
        var item = engine.Registry.Prototypes["item"]["test-item"];
        item.ExtraFields["value"].ToString().Should().Be("2");
    }

    [Fact]
    public async Task Require_ReturnsModuleValue() {
        var info = new FactorioModInfo
            { Name = "test-mod", Version = new Version(1, 0, 0), Title = "test", Author = "test" };
        var mod = new InMemoryFactorioMod(info);
        mod.AddFile("module.lua", "return { value = 42 }");
        mod.AddFile("data.lua",
            "local module = require('__test-mod__.module'); data:extend({{type = 'item', name = 'required-item', value = module.value}})");

        using var engine = new FactorioLuaEngine(new[] { mod });

        await engine.RunAllStages();

        engine.Registry.Prototypes["item"]["required-item"].ExtraFields["value"].ToString().Should().Be("42");
    }

    [Fact]
    public async Task Require_CachesRelativeModulesPerMod() {
        var firstInfo = new FactorioModInfo
            { Name = "first", Version = new Version(1, 0, 0), Title = "first", Author = "test" };
        var first = new InMemoryFactorioMod(firstInfo);
        first.AddFile("module.lua", "return { value = 1 }");
        first.AddFile("data.lua",
            "local module = require('module'); data:extend({{type = 'item', name = 'first', value = module.value}})");

        var secondInfo = new FactorioModInfo
            { Name = "second", Version = new Version(1, 0, 0), Title = "second", Author = "test" };
        var second = new InMemoryFactorioMod(secondInfo);
        second.AddFile("module.lua", "return { value = 2 }");
        second.AddFile("data.lua",
            "local module = require('module'); data:extend({{type = 'item', name = 'second', value = module.value}})");

        using var engine = new FactorioLuaEngine(new[] { first, second });

        await engine.RunAllStages();

        engine.Registry.Prototypes["item"]["first"].ExtraFields["value"].ToString().Should().Be("1");
        engine.Registry.Prototypes["item"]["second"].ExtraFields["value"].ToString().Should().Be("2");
    }

    [Fact]
    public async Task RunAllStages_UsesGenericPrototypeForAbstractPrototypeTypes() {
        var info = new FactorioModInfo
            { Name = "test-mod", Version = new Version(1, 0, 0), Title = "test", Author = "test" };
        var mod = new InMemoryFactorioMod(info);
        mod.AddFile("data.lua", "data:extend({{type = 'loader', name = 'test-loader'}})");

        using var engine = new FactorioLuaEngine(new[] { mod });

        await engine.RunAllStages();

        engine.Registry.Prototypes["loader"].Should().ContainKey("test-loader");
    }

    [Fact]
    public async Task RunAllStages_ExposesSettingDefaultsToDataStage() {
        var info = new FactorioModInfo
            { Name = "test-mod", Version = new Version(1, 0, 0), Title = "test", Author = "test" };
        var mod = new InMemoryFactorioMod(info);
        mod.AddFile("settings.lua",
            "data:extend({{type = 'int-setting', name = 'test-setting', setting_type = 'startup', default_value = 42}})");
        mod.AddFile("data.lua",
            "data:extend({{type = 'item', name = 'configured-item', value = settings.startup['test-setting'].value}})");

        using var engine = new FactorioLuaEngine(new[] { mod });

        await engine.RunAllStages();

        engine.Registry.Prototypes["item"]["configured-item"].ExtraFields["value"].ToString().Should().Be("42");
    }
}