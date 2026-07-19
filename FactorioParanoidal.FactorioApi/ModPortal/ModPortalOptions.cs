namespace FactorioParanoidal.FactorioApi.ModPortal;

public sealed class ModPortalOptions {
    public Uri BaseAddress { get; set; } = new("https://mods.factorio.com/");
    public string? Username { get; set; }
    public string? Token { get; set; }
}