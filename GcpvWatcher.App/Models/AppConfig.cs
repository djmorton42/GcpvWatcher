using System.Text.Json.Serialization;

namespace GcpvWatcher.App.Models;

public class AppConfig
{
    [JsonPropertyName("GcpvExportFilePattern")]
    public string GcpvExportFilePattern { get; set; } = string.Empty;

    [JsonPropertyName("KeyFields")]
    public Dictionary<string, KeyFieldConfig> KeyFields { get; set; } = new();

    [JsonPropertyName("NotificationSoundPath")]
    public string? NotificationSoundPath { get; set; }

    [JsonPropertyName("OutputEncoding")]
    public string OutputEncoding { get; set; } = "ascii";

    [JsonPropertyName("EvtBackupDirectory")]
    public string EvtBackupDirectory { get; set; } = "backups";

    [JsonPropertyName("EnableNotificationSound")]
    public bool EnableNotificationSound { get; set; } = true;

    [JsonPropertyName("FileWatcherStrategy")]
    public string FileWatcherStrategy { get; set; } = "event";

    /// <summary>
    /// When true, single-racer races are merged into the previous race in the same numerical series when building Lynx.evt.
    /// Set at runtime from user preferences; not loaded from appconfig.json.
    /// </summary>
    [JsonIgnore]
    public bool ConsolidateSingleRacerRaces { get; set; }
}

public class AppConfigDto
{
    [JsonPropertyName("GcpvExportFilePattern")]
    public string GcpvExportFilePattern { get; set; } = string.Empty;

    [JsonPropertyName("NotificationSoundPath")]
    public string? NotificationSoundPath { get; set; }

    [JsonPropertyName("KeyFields")]
    public Dictionary<string, KeyFieldConfigDto> KeyFields { get; set; } = new();

    [JsonPropertyName("OutputEncoding")]
    public string OutputEncoding { get; set; } = "ascii";

    [JsonPropertyName("EvtBackupDirectory")]
    public string EvtBackupDirectory { get; set; } = "backups";

    [JsonPropertyName("EnableNotificationSound")]
    public bool EnableNotificationSound { get; set; } = true;

    [JsonPropertyName("FileWatcherStrategy")]
    public string FileWatcherStrategy { get; set; } = "event";
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
