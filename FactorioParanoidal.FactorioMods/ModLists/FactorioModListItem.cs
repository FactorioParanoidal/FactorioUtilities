using System.Text.Json.Serialization;

namespace FactorioParanoidal.FactorioMods.ModLists;

public partial class FactorioModListItem
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}