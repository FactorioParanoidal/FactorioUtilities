namespace FactorioParanoidal.FactorioMods.Mods;

public class FactorioModLoadException : Exception {
    public FactorioModLoadException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}