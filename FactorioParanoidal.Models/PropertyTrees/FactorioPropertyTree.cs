namespace FactorioParanoidal.Models.PropertyTrees;

public class FactorioPropertyTree {
    internal FactorioPropertyTree(FactorioPropertyTreeType type, object? value, bool anyTypeFlag = false) {
        Type = type;
        Value = value;
        AnyTypeFlag = anyTypeFlag;
    }

    public static FactorioPropertyTree CreateNone(bool anyTypeFlag = false)
        => new(FactorioPropertyTreeType.None, null, anyTypeFlag);

    public static FactorioPropertyTree Create(double value, bool anyTypeFlag = false)
        => new(FactorioPropertyTreeType.Number, value, anyTypeFlag);

    public static FactorioPropertyTree Create(bool value, bool anyTypeFlag = false)
        => new(FactorioPropertyTreeType.Bool, value, anyTypeFlag);

    public static FactorioPropertyTree Create(string value, bool anyTypeFlag = false)
        => new(FactorioPropertyTreeType.String, value, anyTypeFlag);

    public static FactorioPropertyTree Create(IEnumerable<FactorioPropertyTree> value, bool anyTypeFlag = false)
        => new(FactorioPropertyTreeType.List, value.ToList(), anyTypeFlag);

    public static FactorioPropertyTree Create(IReadOnlyDictionary<string, FactorioPropertyTree> value,
        bool anyTypeFlag = false)
        => new(FactorioPropertyTreeType.Dictionary, value.ToDictionary(), anyTypeFlag);

    public FactorioPropertyTreeType Type { get; }
    public object? Value { get; }

    /// <remarks>
    /// 1 bool the any-type flag (currently not important outside of Factorio internals; default value is false)
    /// according to https://wiki.factorio.com/Property_tree
    /// </remarks>
    public bool AnyTypeFlag { get; }

    public bool AsBool()
        => (bool)Value!;

    public double AsNumber()
        => (double)Value!;

    public string AsString()
        => (string)Value!;

    public IReadOnlyList<FactorioPropertyTree> AsList()
        => (IReadOnlyList<FactorioPropertyTree>)Value!;

    public IReadOnlyDictionary<string, FactorioPropertyTree> AsDictionary()
        => (IReadOnlyDictionary<string, FactorioPropertyTree>)Value!;
}