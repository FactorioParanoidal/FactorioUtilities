namespace FactorioParanoidal.FactorioMods.Execution.Models;

public readonly struct ModFileReference {
    public string? Mod { get; }
    public string Path { get; }

    public ModFileReference(string? mod, string path) {
        Mod = mod;
        Path = path;
    }

    private ModFileReference(string reference) {
        if (reference.Split('/', '\\').Contains("..", StringComparer.Ordinal)) {
            throw new ArgumentException("Parent directory references are not allowed in Factorio require paths.",
                nameof(reference));
        }

        if (!reference.EndsWith(".lua")) {
            reference = reference.Replace('.', '/') + ".lua";
        }

        if (reference.StartsWith("__")) {
            var secondDoubleUnderscore = reference.IndexOf("__", 2, StringComparison.InvariantCulture);
            if (secondDoubleUnderscore != -1) {
                Mod = reference[2..secondDoubleUnderscore];
                reference = reference[(secondDoubleUnderscore + 2)..].TrimStart('/');
            }
        }

        Path = reference;
    }

    public static ModFileReference FromRequire(string reference) {
        return new ModFileReference(reference);
    }

    public string Folder => System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
}