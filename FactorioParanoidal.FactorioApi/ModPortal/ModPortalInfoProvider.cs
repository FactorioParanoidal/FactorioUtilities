using System.Net.Http.Json;
using System.Text.Json;
using FactorioParanoidal.FactorioApi.Models.Data;
using FactorioParanoidal.FactorioApi.Models.Requests;
using FactorioParanoidal.FactorioApi.ModPortal.Models.Requests;
using FactorioParanoidal.FactorioApi.ModPortal.Models.Responses;

namespace FactorioParanoidal.FactorioApi.ModPortal;

public sealed class ModPortalInfoProvider(HttpClient client) : IModInfoProvider {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ModPage> GetModsAsync(ModQuery? query = null, CancellationToken cancellationToken = default) {
        query ??= new();
        var request = new ModPortalModsRequest(query.Page, query.PageSize, query.FactorioVersion,
            query.HideDeprecated, query.Sort, query.SortOrder, query.Names);
        var parameters = new List<string> {
            $"page={request.Page}", $"page_size={request.PageSize}",
            $"hide_deprecated={request.HideDeprecated.ToString().ToLowerInvariant()}",
            $"sort={Uri.EscapeDataString(request.Sort)}", $"sort_order={Uri.EscapeDataString(request.SortOrder)}"
        };
        if (request.Version is not null) parameters.Add($"version={Uri.EscapeDataString(request.Version)}");
        if (request.Names is { Count: > 0 })
            parameters.AddRange(request.Names.Select(n => $"namelist={Uri.EscapeDataString(n)}"));
        var response =
            await GetJsonAsync<ModPortalResponse>($"api/mods?{string.Join('&', parameters)}", cancellationToken);
        return response.ToModel();
    }

    public async Task<Mod> GetModAsync(string name, bool full = false, CancellationToken cancellationToken = default) {
        var response = await GetJsonAsync<ModPortalModResponse>(
            $"api/mods/{Uri.EscapeDataString(name)}{(full ? "/full" : string.Empty)}", cancellationToken);
        return response.ToModel();
    }

    private async Task<T> GetJsonAsync<T>(string uri, CancellationToken cancellationToken) {
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ModPortalHttpException(response.StatusCode, response.RequestMessage?.RequestUri?.ToString(),
                message);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
               ?? throw new JsonException("Mod Portal returned an empty response.");
    }
}