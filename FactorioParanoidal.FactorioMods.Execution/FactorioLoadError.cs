namespace FactorioParanoidal.FactorioMods.Execution;

public sealed record FactorioLoadError(
    FactorioDataStage Stage,
    string ModName,
    string FileName,
    Exception Exception);