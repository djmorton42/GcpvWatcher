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
        ApplicationLogger.Log($"Looking for configuration file at: {_configPath}");
        if (!File.Exists(_configPath))
            throw new FileNotFoundException($"Configuration file '{_configPath}' not found.");

        var jsonContent = await File.ReadAllTextAsync(_configPath);
        var configDto = JsonSerializer.Deserialize<AppConfigDto>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (configDto == null)
            throw new InvalidOperationException($"Failed to deserialize configuration from '{_configPath}'.");

        return ConvertToAppConfig(configDto);
    }

    public AppConfig LoadConfig()
    {
        if (!File.Exists(_configPath))
            throw new FileNotFoundException($"Configuration file '{_configPath}' not found.");

        var jsonContent = File.ReadAllText(_configPath);
        var configDto = JsonSerializer.Deserialize<AppConfigDto>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (configDto == null)
            throw new InvalidOperationException($"Failed to deserialize configuration from '{_configPath}'.");

        return ConvertToAppConfig(configDto);
    }

    private AppConfig ConvertToAppConfig(AppConfigDto dto)
    {
        var keyFields = new Dictionary<string, KeyFieldConfig>();
        foreach (var kvp in dto.KeyFields)
        {
            keyFields[kvp.Key] = new KeyFieldConfig(
                kvp.Value.Key,
                kvp.Value.Offset,
                kvp.Value.SuffixStopWords
            );
        }

        return new AppConfig
        {
            GcpvExportFilePattern = dto.GcpvExportFilePattern,
            NotificationSoundPath = dto.NotificationSoundPath,
            KeyFields = keyFields
        };
    }
}
