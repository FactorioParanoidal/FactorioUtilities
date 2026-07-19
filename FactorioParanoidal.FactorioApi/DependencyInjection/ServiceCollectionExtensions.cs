using System.Net;
using FactorioParanoidal.FactorioApi.ModPortal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FactorioParanoidal.FactorioApi.DependencyInjection;

public static class ServiceCollectionExtensions {
    public static FactorioApiBuilder AddFactorioModPortal(this IServiceCollection services) {
        services.AddOptions<FactorioApiOptions>();
        services.AddOptions<ModPortalOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<FactorioApiOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<ModPortalOptions>>().Value);
        services.AddTransient<IModInfoProvider>(sp => new ModPortalInfoProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("FactorioModPortal")));
        services.AddTransient<IModDownloadProvider>(sp => new ModPortalDownloadProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("FactorioModPortal"),
            sp.GetRequiredService<ModPortalOptions>()));
        services.AddTransient<IFactorioApi, ModPortal.ModPortal>();
        var builder = services.AddHttpClient("FactorioModPortal", (sp, client) => {
            var options = sp.GetRequiredService<FactorioApiOptions>();
            var modPortalOptions = sp.GetRequiredService<ModPortalOptions>();
            client.BaseAddress = modPortalOptions.BaseAddress;
            client.Timeout = options.RequestTimeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FactorioParanoidal.FactorioApi/1.0");
        });
        builder.AddStandardResilienceHandler(resilience => {
            resilience.Retry.MaxRetryAttempts = 3;
            resilience.Retry.Delay = TimeSpan.FromSeconds(1);
            resilience.Retry.UseJitter = true;
            resilience.Retry.ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
                (int?)args.Outcome.Result?.StatusCode >= 500 || args.Outcome.Exception is HttpRequestException);
        });

        return new FactorioApiBuilder(services);
    }
}