namespace FactorioParanoidal.FactorioApi;

public sealed class FactorioApiOptions {
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(100);
}