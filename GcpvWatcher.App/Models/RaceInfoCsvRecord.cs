using CsvHelper.Configuration.Attributes;

namespace GcpvWatcher.App.Models;

public class RaceInfoCsvRecord
{
    [Index(0)]
    public string RaceNumber { get; set; } = string.Empty;

    [Index(1)]
    public string Field1 { get; set; } = string.Empty;

    [Index(2)]
    public string Field2 { get; set; } = string.Empty;

    [Index(3)]
    public string RaceTitle { get; set; } = string.Empty;

    [Index(4)]
    public string Field4 { get; set; } = string.Empty;

    [Index(5)]
    public string Field5 { get; set; } = string.Empty;

    [Index(6)]
    public string Field6 { get; set; } = string.Empty;

    [Index(7)]
    public string Field7 { get; set; } = string.Empty;

    [Index(8)]
    public string Field8 { get; set; } = string.Empty;

    [Index(9)]
    public string Field9 { get; set; } = string.Empty;

    [Index(10)]
    public string Field10 { get; set; } = string.Empty;

    [Index(11)]
    public string Field11 { get; set; } = string.Empty;

    [Index(12)]
    public string NumberOfLaps { get; set; } = string.Empty;
}
