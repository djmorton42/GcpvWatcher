using GcpvWatcher.App.Models;
using System.Text.Json;

namespace GcpvWatcher.App.Services;

public class AppConfigService
{
    private readonly string _configPath;

    public AppConfigService(string configPath = "appconfig.json")
    {
        _configPath = configPath;
    }

    public async Task<AppConfig> LoadConfigAsync()
    {
        if (!File.Exists(_configPath))
            throw new FileNotFoundException($"Configuration file '{_configPath}' not found.");

        var jsonContent = await File.ReadAllTextAsync(_configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config == null)
            throw new InvalidOperationException($"Failed to deserialize configuration from '{_configPath}'.");

        return config;
    }

    public AppConfig LoadConfig()
    {
        if (!File.Exists(_configPath))
            throw new FileNotFoundException($"Configuration file '{_configPath}' not found.");

        var jsonContent = File.ReadAllText(_configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config == null)
            throw new InvalidOperationException($"Failed to deserialize configuration from '{_configPath}'.");

        return config;
    }
}
