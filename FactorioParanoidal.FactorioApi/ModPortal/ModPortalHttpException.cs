using System.Net;

namespace FactorioParanoidal.FactorioApi.ModPortal;

public sealed class ModPortalHttpException(HttpStatusCode statusCode, string? requestUri, string? responseBody = null)
    : HttpRequestException($"Mod Portal returned {(int)statusCode} ({statusCode}) for {requestUri}.", null,
        statusCode) {
    public string? ResponseBody { get; } = responseBody;
}