using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.ModLists;

public partial class FactorioModList {
    [JsonPropertyName("mods")] public FactorioModListItem[] Mods { get; set; } = Array.Empty<FactorioModListItem>();
}