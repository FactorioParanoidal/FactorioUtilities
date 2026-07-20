namespace FactorioParanoidal.FactorioMods.Execution;

public sealed class FactorioStageResult {
    internal FactorioStageResult(FactorioDataStage stage, PrototypeRegistry registry,
        IReadOnlyList<FactorioLoadError> errors) {
        Stage = stage;
        Registry = registry;
        Errors = errors;
    }

    public FactorioDataStage Stage { get; }
    public PrototypeRegistry Registry { get; }
    public IReadOnlyList<FactorioLoadError> Errors { get; }
    public bool IsSuccessful => Errors.Count == 0;
}