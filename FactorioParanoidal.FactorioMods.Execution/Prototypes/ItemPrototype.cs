using System.Text.Json.Serialization;
using FactorioParanoidal.FactorioMods.Execution.SourceGenerator;

namespace FactorioParanoidal.FactorioMods.Execution.Prototypes;

[LuaExtraObject]
public partial class ItemPrototype : FactorioPrototype {
    [LuaExtraMember]
    [JsonPropertyName("stack_size")]
    public int StackSize { get; set; }
}