using System.Text.Json.Serialization;
using FactorioParanoidal.FactorioMods.Execution.SourceGenerator;

namespace FactorioParanoidal.FactorioMods.Execution.Prototypes;

[LuaExtraObject]
public partial class FactorioPrototype {
    [LuaExtraMember]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [LuaExtraMember]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    // We use object to store generic dynamic values coming from Lua that don't map to typed properties
    [LuaExtraFields] [JsonExtensionData] public Dictionary<string, object> ExtraFields { get; set; } = new();
}