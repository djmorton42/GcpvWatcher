using System.Text.Json.Serialization;

namespace GcpvWatcher.App.Models;

public class AppConfig
{
    [JsonPropertyName("GcpvExportFilePattern")]
    public string GcpvExportFilePattern { get; set; } = string.Empty;

    [JsonPropertyName("KeyFields")]
    public Dictionary<string, KeyFieldConfig> KeyFields { get; set; } = new();
}

public class AppConfigDto
{
    [JsonPropertyName("GcpvExportFilePattern")]
    public string GcpvExportFilePattern { get; set; } = string.Empty;

    [JsonPropertyName("KeyFields")]
    public Dictionary<string, KeyFieldConfigDto> KeyFields { get; set; } = new();
}

public class KeyFieldConfigDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("suffix_stop_words")]
    public string[]? SuffixStopWords { get; set; }
}
