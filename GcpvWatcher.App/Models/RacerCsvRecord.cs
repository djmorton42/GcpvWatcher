using CsvHelper.Configuration.Attributes;

namespace GcpvWatcher.App.Models;

public class RacerCsvRecord
{
    [Index(0)]
    public string RacerId { get; set; } = string.Empty;

    [Index(1)]
    public string LastName { get; set; } = string.Empty;

    [Index(2)]
    public string FirstName { get; set; } = string.Empty;

    [Index(3)]
    public string Affiliation { get; set; } = string.Empty;
}
