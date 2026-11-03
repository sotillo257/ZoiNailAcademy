using Microsoft.AspNetCore.HttpOverrides;

namespace ZoiNailAcademy.Host.Networking;

/// <summary>
/// Section <c>ForwardedHeaders</c>. Production chain: browser → Railway edge → Next.js → private
/// network → API. <see cref="ForwardLimit"/> must equal the number of proxies that append to
/// X-Forwarded-For; a larger value lets a client spoof its IP while every proxy is trusted.
/// Verify with <c>GET /api/_diag/ip</c> (Diag__Enabled=true) and then disable it.
/// </summary>
public class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public int ForwardLimit { get; set; } = 2;

    /// <summary>Trusted proxy networks in CIDR notation. Empty = trust every proxy (only while the API has no public domain).</summary>
    public string[] KnownNetworks { get; set; } = [];

    public void Apply(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = ForwardLimit;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var network in KnownNetworks.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network.Trim()));
        }
    }

    public bool TrustsAnyProxy => !KnownNetworks.Any(n => !string.IsNullOrWhiteSpace(n));
}
