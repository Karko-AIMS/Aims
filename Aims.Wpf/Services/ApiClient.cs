using System.Configuration;
using System.Net.Http;

namespace Aims.Wpf.Services;

public sealed class ApiClient
{
    public HttpClient Http { get; }

    public ApiClient()
    {
        var baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"]
                      ?? throw new InvalidOperationException("Missing ApiBaseUrl in App.config");

        Http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
    }
}
