using System.Text.Json.Serialization;

namespace FactorioParanoidal.Models.Prototypes;

public class ItemPrototype : FactorioPrototype {
    [JsonPropertyName("stack_size")] public int StackSize { get; set; }
}