using Microsoft.AspNetCore.Http;
using Voxa.Abstractions.Services;

namespace Voxa.Services;

/// <summary>
/// Reads the client's address and device id from the request. Behind Cloudflare every request comes from a Cloudflare
/// address, so CF-Connecting-IP is used when present; the server must only accept traffic from Cloudflare
/// (docs/deployment.md), otherwise a client could set it.
/// </summary>
public sealed class ClientIdentity(IHttpContextAccessor accessor) : IClientIdentity
{
    public const string DeviceHeader = "X-Voxa-Device";

    public string Address
    {
        get
        {
            var context = accessor.HttpContext;
            var cloudflare = context?.Request.Headers["CF-Connecting-IP"].ToString();
            return !string.IsNullOrWhiteSpace(cloudflare) ? cloudflare : context?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public string Device
    {
        get
        {
            var device = accessor.HttpContext?.Request.Headers[DeviceHeader].ToString();
            return string.IsNullOrWhiteSpace(device) || device.Length > 64 ? "address:" + Address : "device:" + device;
        }
    }
}
