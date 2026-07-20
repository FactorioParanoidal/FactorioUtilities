namespace FactorioParanoidal.FactorioMods.Mods;

public sealed class FactorioVersion : IEquatable<FactorioVersion>, IComparable<FactorioVersion> {
    private readonly string _text;
    private readonly Version _version;

    public FactorioVersion(string version) {
        ArgumentNullException.ThrowIfNull(version);

        _version = new Version(version);
        _text = version;
    }

    public FactorioVersion(Version version) {
        ArgumentNullException.ThrowIfNull(version);

        _version = version;
        _text = version.ToString();
    }

    public int Major => _version.Major;
    public int Minor => _version.Minor;
    public int Build => _version.Build;
    public int Revision => _version.Revision;

    public int CompareTo(FactorioVersion? other) => other is null ? 1 : _version.CompareTo(other._version);

    public bool Equals(FactorioVersion? other) => other is not null && _version.Equals(other._version);

    public override bool Equals(object? obj) => obj switch {
        FactorioVersion other => Equals(other),
        Version other => _version.Equals(other),
        _ => false
    };

    public override int GetHashCode() => _version.GetHashCode();

    public override string ToString() => _text;

    public string ToString(int fieldCount) {
        return _text.Count(character => character == '.') + 1 == fieldCount
            ? _text
            : _version.ToString(fieldCount);
    }

    public static implicit operator Version(FactorioVersion version) => version._version;

    public static implicit operator FactorioVersion(Version version) => new(version);

    public static bool operator ==(FactorioVersion? left, FactorioVersion? right) => Equals(left, right);

    public static bool operator !=(FactorioVersion? left, FactorioVersion? right) => !Equals(left, right);

    public static bool operator <(FactorioVersion left, FactorioVersion right) => left.CompareTo(right) < 0;

    public static bool operator <=(FactorioVersion left, FactorioVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >(FactorioVersion left, FactorioVersion right) => left.CompareTo(right) > 0;

    public static bool operator >=(FactorioVersion left, FactorioVersion right) => left.CompareTo(right) >= 0;
}