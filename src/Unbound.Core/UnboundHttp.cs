using System.Net;

namespace Unbound.Core;

public static class UnboundHttp
{
    /// <summary>
    /// Modrinth requires a unique, identifying User-Agent (a contact is encouraged). Update the URL
    /// to your real repo before publishing.
    /// </summary>
    public const string UserAgent = "unbound-installer/0.1.0 (+https://github.com/your-org/unbound)";

    public static HttpClient Create()
    {
        var http = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        })
        {
            Timeout = TimeSpan.FromSeconds(100),
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return http;
    }
}
