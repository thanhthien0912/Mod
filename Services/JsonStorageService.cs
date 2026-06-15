using System.Text.Json;
using GtavOfflineModLauncher.Helpers;
using GtavOfflineModLauncher.Models;

namespace GtavOfflineModLauncher.Services;

public sealed class JsonStorageService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<AppSettings> LoadAppSettingsAsync()
    {
        var path = PathHelper.AppSettingsPath;
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, _jsonOptions) ?? new AppSettings();
    }

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        var path = PathHelper.AppSettingsPath;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, settings, _jsonOptions);
    }

    public async Task<InstalledModsFile> LoadInstalledModsAsync()
    {
        var path = PathHelper.InstalledModsPath;
        if (!File.Exists(path))
        {
            return new InstalledModsFile();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<InstalledModsFile>(stream, _jsonOptions) ?? new InstalledModsFile();
    }

    public async Task SaveInstalledModsAsync(InstalledModsFile installedMods)
    {
        Directory.CreateDirectory(PathHelper.AppDataRoot);

        await using var stream = File.Create(PathHelper.InstalledModsPath);
        await JsonSerializer.SerializeAsync(stream, installedMods, _jsonOptions);
    }
}
