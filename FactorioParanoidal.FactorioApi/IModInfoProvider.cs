using FactorioParanoidal.FactorioApi.Models.Data;
using FactorioParanoidal.FactorioApi.Models.Requests;

namespace FactorioParanoidal.FactorioApi;

public interface IModInfoProvider {
    Task<ModPage> GetModsAsync(ModQuery? query = null, CancellationToken cancellationToken = default);
    Task<Mod> GetModAsync(string name, bool full = false, CancellationToken cancellationToken = default);
}