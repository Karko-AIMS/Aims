using System.Net.Http.Json;

namespace Aims.Wpf.Services;

public sealed class AuthService
{
    private readonly ApiClient _api;
    private readonly ITokenStore _tokenStore;

    public AuthService(ApiClient api, ITokenStore tokenStore)
    {
        _api = api;
        _tokenStore = tokenStore;
    }

    // ---------- LOGIN ----------
    public sealed record LoginRequest(string Email, string Password);

    public sealed record LoginResponse(
        string accessToken,
        string tokenType,
        int expiresIn,
        string role
    );

    public async Task<LoginResponse> LoginAsync(string email, string password, bool rememberMe)
    {
        var resp = await _api.Http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        var data = await resp.Content.ReadFromJsonAsync<LoginResponse>()
                   ?? throw new InvalidOperationException("Login response is empty");

        await _tokenStore.SetAsync(data.accessToken, rememberMe);
        _api.ApplyAuthHeader();

        return data;
    }

    public async Task LogoutAsync()
    {
        await _tokenStore.ClearAsync();
        _api.ApplyAuthHeader();
    }

    // ---------- ME ----------
    public sealed record MeResponse(
        string? userId,
        string? email,
        string? role,
        string? orgId
    );

    public async Task<MeResponse> MeAsync()
    {
        var resp = await _api.Http.GetAsync("api/auth/me");

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Me failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        var data = await resp.Content.ReadFromJsonAsync<MeResponse>()
                   ?? throw new InvalidOperationException("Me response is empty");

        return data;
    }

    // ---------- REGISTER ----------
    public sealed record RegisterRequest(
        string Email,
        string Password,
        Guid OrgId,
        int Role
    );

    public sealed record RegisterResponse(
        Guid id,
        string email,
        Guid orgId,
        string role,
        bool isActive,
        DateTime createdAtUtc
    );

    public async Task<RegisterResponse> RegisterAsync(string email, string password, Guid orgId, int role)
    {
        var resp = await _api.Http.PostAsJsonAsync(
            "api/auth/register",
            new RegisterRequest(email, password, orgId, role)
        );

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
        }

        var data = await resp.Content.ReadFromJsonAsync<RegisterResponse>()
                   ?? throw new InvalidOperationException("Register response is empty");

        return data;
    }
}
