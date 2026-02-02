using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Aims.Wpf.Services;

public interface ITokenStore
{
    string? CurrentToken { get; }
    bool HasToken { get; }

    Task LoadAsync();
    Task SetAsync(string token, bool rememberMe);
    Task ClearAsync();
}

public sealed class TokenStore : ITokenStore
{
    private readonly string _filePath;
    private string? _token;

    public TokenStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Aims.Wpf"
        );
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "token.dat");
    }

    public string? CurrentToken => _token;
    public bool HasToken => !string.IsNullOrWhiteSpace(_token);

    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            _token = null;
            return;
        }

        var protectedBytes = await File.ReadAllBytesAsync(_filePath);
        var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        _token = Encoding.UTF8.GetString(bytes);
    }

    public async Task SetAsync(string token, bool rememberMe)
    {
        _token = token;

        if (!rememberMe)
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(token);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(_filePath, protectedBytes);
    }

    public Task ClearAsync()
    {
        _token = null;

        if (File.Exists(_filePath))
            File.Delete(_filePath);

        return Task.CompletedTask;
    }
}
