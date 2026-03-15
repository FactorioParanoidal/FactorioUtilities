using FactorioParanoidal.FactorioMods.Execution;
using FactorioParanoidal.FactorioMods.Mods;
using FluentAssertions;
using Xunit;

namespace FactorioParanoidal.FactorioMods.Execution.Tests;

public class FactorioModuleLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _baseDir;

    public FactorioModuleLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _baseDir = Path.Combine(_tempDir, "base");
        Directory.CreateDirectory(_baseDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void ResolvePath_WithModPrefix_ResolvesCorrectly()
    {
        // Arrange
        var info = new FactorioModInfo { Name = "base", Version = new Version(1, 0, 0), Title = "base", Author = "Wube" };
        var baseMod = new FolderFactorioMod(info, _baseDir);
        var loader = new FactorioModuleLoader(new[] { baseMod });

        var luaFilePath = Path.Combine(_baseDir, "prototypes", "entity.lua");
        Directory.CreateDirectory(Path.GetDirectoryName(luaFilePath)!);
        File.WriteAllText(luaFilePath, "return {}");

        // Act & Assert
        loader.Exists("__base__.prototypes.entity").Should().BeTrue();
        loader.Exists("__base__/prototypes/entity.lua").Should().BeTrue();
    }
}
