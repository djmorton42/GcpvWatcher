using GcpvWatcher.App.Models;
using System.Text;
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

        var config = ConvertToAppConfig(configDto);
        ValidateOutputEncoding(config.OutputEncoding);
        return config;
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

        var config = ConvertToAppConfig(configDto);
        ValidateOutputEncoding(config.OutputEncoding);
        return config;
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
            OutputEncoding = dto.OutputEncoding,
            EvtBackupDirectory = dto.EvtBackupDirectory,
            EnableNotificationSound = dto.EnableNotificationSound,
            KeyFields = keyFields
        };
    }

    /// <summary>
    /// Gets the encoding to use for writing EVT files based on configuration
    /// </summary>
    public static Encoding GetOutputEncoding(string outputEncoding)
    {
        if (string.IsNullOrEmpty(outputEncoding))
            return Encoding.ASCII; // Default to ASCII for null/empty values
            
        return outputEncoding.ToLowerInvariant() switch
        {
            "utf-16" => Encoding.Unicode,
            "utf-8" => Encoding.UTF8,
            "ascii" => Encoding.ASCII,
            _ => Encoding.ASCII // Default to ASCII for unknown values
        };
    }

    /// <summary>
    /// Validates the OutputEncoding property and throws exception if invalid
    /// </summary>
    private static void ValidateOutputEncoding(string outputEncoding)
    {
        var validEncodings = new[] { "utf-8", "utf-16", "ascii" };
        var normalizedEncoding = outputEncoding?.ToLowerInvariant();
        
        if (string.IsNullOrEmpty(normalizedEncoding) || !validEncodings.Contains(normalizedEncoding))
        {
            var errorMessage = $"Invalid OutputEncoding value in appconfig.json. " +
                              $"Current value: '{outputEncoding}'. " +
                              $"Valid values are: {string.Join(", ", validEncodings)}";
            throw new InvalidOperationException(errorMessage);
        }
    }
}
