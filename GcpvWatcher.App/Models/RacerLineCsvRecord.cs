using CsvHelper.Configuration.Attributes;

namespace GcpvWatcher.App.Models;

public class RacerLineCsvRecord
{
    [Index(0)]
    public string Field1 { get; set; } = string.Empty;

    [Index(1)]
    public string RacerId { get; set; } = string.Empty;

    [Index(2)]
    public string Lane { get; set; } = string.Empty;
}
