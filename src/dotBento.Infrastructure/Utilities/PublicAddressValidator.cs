using System.Net;
using System.Net.Sockets;

namespace dotBento.Infrastructure.Utilities;

/// <summary>
/// Classifies IP addresses that a public image URL must never resolve to.
/// This is the application-level SSRF guard; the outbound HTTP handler also
/// connects to the validated address so a DNS answer cannot change between the
/// check and the connection.
/// </summary>
public static class PublicAddressValidator
{
    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsPublicIPv4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsPublicIPv6(address.GetAddressBytes()),
            _ => false,
        };
    }

    /// <summary>Returns true only if every address is public and at least one exists.</summary>
    public static bool AllPublic(IEnumerable<IPAddress> addresses)
    {
        var any = false;
        foreach (var address in addresses)
        {
            any = true;
            if (!IsPublic(address)) return false;
        }

        return any;
    }

    private static bool IsPublicIPv4(byte[] bytes)
    {
        if (bytes[0] is 0 or 10 or 127 || bytes[0] >= 224) return false;

        // Carrier-grade NAT (also used by some cloud metadata services).
        if (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) return false;

        // Link-local, including 169.254.169.254.
        if (bytes[0] == 169 && bytes[1] == 254) return false;

        if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) return false;
        if (bytes[0] == 192 && bytes[1] == 168) return false;

        // IETF protocol assignments and documentation ranges.
        if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] is 0 or 2) return false;
        if (bytes[0] == 198 && bytes[1] is 18 or 19) return false;
        if (bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100) return false;
        if (bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113) return false;

        return true;
    }

    private static bool IsPublicIPv6(byte[] bytes)
    {
        // Only global unicast (2000::/3). This excludes loopback, link-local,
        // unique-local, multicast, NAT64, and the unspecified address.
        if ((bytes[0] & 0xE0) != 0x20) return false;

        // Documentation (2001:db8::/32), Teredo (2001::/32), and 6to4
        // (2002::/16) are not useful image hosts and broaden the SSRF surface.
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8) return false;
        if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x00 && bytes[3] == 0x00) return false;
        if (bytes[0] == 0x20 && bytes[1] == 0x02) return false;

        return true;
    }
}
