using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Voxa.WebApi;

/// <summary>
/// The API runs behind a reverse proxy (Traefik in docker-compose-production.yml). Only that proxy may tell the API the
/// client's address and scheme, so forwarded headers are trusted from the private ranges Docker networks use and from
/// any extra networks listed in <c>ForwardedHeaders:KnownNetworks</c>.
/// </summary>
public static class ForwardedHeadersConfiguration
{
    private static readonly string[] s_dockerNetworks = ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];

    public static IServiceCollection AddVoxaForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var extra = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var network in s_dockerNetworks.Concat(extra))
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }

            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
        });
    }
}
