using System.Text.Json.Serialization;
using FactorioParanoidal.Models.PropertyTrees;

namespace FactorioParanoidal.ModSettingsDat;

public class ModSettingsContent {
    [JsonPropertyName("startup")]
    public Dictionary<string, FactorioPropertyTree> Startup { get; set; } = new();

    [JsonPropertyName("runtime-global")]
    public Dictionary<string, FactorioPropertyTree> RuntimeGlobal { get; set; } = new();

    [JsonPropertyName("runtime-per-user")]
    public Dictionary<string, FactorioPropertyTree> RuntimePerUser { get; set; } = new();
}