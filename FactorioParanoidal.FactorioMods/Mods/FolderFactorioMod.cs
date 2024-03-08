using System.Text.Json;

namespace FactorioParanoidal.FactorioMods.Mods;

public class FolderFactorioMod : IFactorioMod {
    public FolderFactorioMod(FactorioModInfo info, string directory) {
        Info = info;
        Directory = directory;
    }
    
    public static async Task<FolderFactorioMod> LoadFromDirectory(string modDirectory) {
        try {
            modDirectory = Path.GetFullPath(modDirectory);
            var infoJson = Path.Combine(modDirectory, IFactorioMod.InfoJsonPath);
            await using var fileStream = File.OpenRead(infoJson);
            var modInfo = await JsonSerializer.DeserializeAsync<FactorioModInfo>(fileStream);
            return new FolderFactorioMod(modInfo!, modDirectory);
        }
        catch (Exception e) {
            throw new FactorioModLoadException(
                $"Factorio mod loading from {modDirectory} failed. See inner exception for details", e);
        }
    }

    public FactorioModInfo Info { get; }

    public string Directory { get; set; }
}