using System.Collections;
using FactorioParanoidal.FactorioMods.Mods;

namespace FactorioParanoidal.FactorioMods;

public class FactorioModpack : IEnumerable<IFactorioMod> {
    private readonly IEnumerable<IFactorioMod> _enumerableImplementation;

    public FactorioModpack(IEnumerable<IFactorioMod> enumerableImplementation) {
        _enumerableImplementation = enumerableImplementation.ToList();
    }

    /// <remarks>
    /// Only loading mods from folders are supported by now
    /// </remarks>
    public static async Task<FactorioModpack> LoadFromDirectory(string directory) {
        var directories = Directory.GetDirectories(directory);
        var loadingTasks = directories.Select(FolderFactorioMod.LoadFromDirectory);
        var mods = await Task.WhenAll(loadingTasks);
        return new FactorioModpack(mods);
    }

    public IEnumerator<IFactorioMod> GetEnumerator() {
        return _enumerableImplementation.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)_enumerableImplementation).GetEnumerator();
    }
}