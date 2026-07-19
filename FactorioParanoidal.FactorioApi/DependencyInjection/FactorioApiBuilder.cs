using FactorioParanoidal.FactorioApi.ModPortal;
using FactorioParanoidal.FactorioApi.Re146;
using Microsoft.Extensions.DependencyInjection;

namespace FactorioParanoidal.FactorioApi.DependencyInjection;

public sealed class FactorioApiBuilder {
    internal FactorioApiBuilder(IServiceCollection services) {
        Services = services;
    }

    internal IServiceCollection Services { get; }

    public FactorioApiBuilder ConfigureApi(Action<FactorioApiOptions> configure) {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }

    public FactorioApiBuilder ConfigureModPortal(Action<ModPortalOptions> configure) {
        ArgumentNullException.ThrowIfNull(configure);
        Services.Configure(configure);
        return this;
    }

    public FactorioApiBuilder UseRe146ModDownloader() {
        Services.AddTransient<IModDownloadProvider>(sp => new Re146ModDownloadProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("FactorioModStorage")));
        Services.AddHttpClient("FactorioModStorage", (sp, client) => {
            client.BaseAddress = new Uri("https://mods-storage.re146.dev/");
            client.Timeout = sp.GetRequiredService<FactorioApiOptions>().RequestTimeout;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FactorioParanoidal.FactorioApi/1.0");
        }).AddStandardResilienceHandler(resilience => {
            resilience.Retry.MaxRetryAttempts = 3;
            resilience.Retry.Delay = TimeSpan.FromSeconds(1);
            resilience.Retry.UseJitter = true;
        });
        return this;
    }
}