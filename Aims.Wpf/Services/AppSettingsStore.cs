using System.IO;
using System.Text.Json;

namespace Aims.Wpf.Services;

public sealed class AppSettingsStore
{
    private readonly string _filePath;

    public AppSettingsStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Aims.Wpf"
        );
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "appsettings.local.json");
    }

    public sealed class LocalSettings
    {
        public bool AutoLogin { get; set; } = false;

        // Remember me 관련: Email만 저장
        public bool RememberEmail { get; set; } = false;
        public string? LastEmail { get; set; } = null;
    }

    public async Task<LocalSettings> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new LocalSettings();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<LocalSettings>(json) ?? new LocalSettings();
    }

    public async Task SaveAsync(LocalSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_filePath, json);
    }
}
