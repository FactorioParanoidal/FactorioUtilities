namespace FactorioParanoidal.FactorioMods.Execution;

public sealed class FactorioLoadResult {
    private readonly IReadOnlyList<FactorioLoadError> _errors;

    internal FactorioLoadResult(IReadOnlyDictionary<FactorioDataStage, FactorioStageResult> stages) {
        Stages = stages;
        _errors = stages.Values.SelectMany(stage => stage.Errors).ToArray();
    }

    public IReadOnlyDictionary<FactorioDataStage, FactorioStageResult> Stages { get; }
    public IReadOnlyList<FactorioLoadError> Errors => _errors;
    public bool IsSuccessful => _errors.Count == 0;

    public void EnsureSuccessful() {
        if (!IsSuccessful) throw new FactorioLoadException(_errors);
    }
}

public sealed class FactorioLoadException : AggregateException {
    public FactorioLoadException(IReadOnlyList<FactorioLoadError> errors)
        : base($"Factorio mod loading failed with {errors.Count} error(s).",
            errors.Select(error => error.Exception)) {
        Errors = errors;
    }

    public IReadOnlyList<FactorioLoadError> Errors { get; }
}