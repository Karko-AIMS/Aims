using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Aims.Wpf.Services;

public sealed class ApiClient
{
    public HttpClient Http { get; }

    private readonly ITokenStore _tokenStore;

    public ApiClient(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;

        var baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"]
                      ?? throw new InvalidOperationException("Missing ApiBaseUrl in App.config");

        Http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };

        ApplyAuthHeader();
    }

    public void ApplyAuthHeader()
    {
        var token = _tokenStore.CurrentToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            Http.DefaultRequestHeaders.Authorization = null;
            return;
        }

        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
