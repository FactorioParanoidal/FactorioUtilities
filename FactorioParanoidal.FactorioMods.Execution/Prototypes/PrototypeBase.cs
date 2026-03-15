using System.Text.Json.Serialization;
using FactorioParanoidal.FactorioMods.Execution.SourceGenerator;

namespace FactorioParanoidal.FactorioMods.Execution.Prototypes;

[LuaExtraObject]
public abstract partial class PrototypeBase {
    // We use object to store generic dynamic values coming from Lua that don't map to typed properties
    [LuaExtraFields] [JsonExtensionData] public Dictionary<string, object> ExtraFields { get; set; } = new();
}