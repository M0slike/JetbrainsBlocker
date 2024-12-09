using System.Net;
using WindowsFirewallHelper.Addresses;

namespace JetbrainsBlocker.Worker.Helpers;

internal static class DnsHelper
{
    public static async Task<List<SingleIP>> LookupUrlIpAddressListAsync(string url, CancellationToken ct = new())
    {
        var hostEntry = await Dns.GetHostEntryAsync(url, ct);
        return hostEntry.AddressList.Select(ip => new SingleIP(ip)).ToList();
    }
}
