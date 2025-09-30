using System.Text.Json.Serialization;

namespace GcpvWatcher.App.Models;

public class AppConfig
{
    [JsonPropertyName("GcpvExportFilePattern")]
    public string GcpvExportFilePattern { get; set; } = string.Empty;

    [JsonPropertyName("KeyFields")]
    public Dictionary<string, KeyFieldConfig> KeyFields { get; set; } = new();
}
