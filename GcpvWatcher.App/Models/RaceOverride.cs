namespace GcpvWatcher.App.Models;

/// <summary>
/// Operator edit for one source race. A null field means "keep the calculated value".
/// </summary>
public sealed class RaceOverride
{
    public string? Title { get; set; }

    public decimal? Laps { get; set; }

    public bool IsEmpty => Title is null && Laps is null;
}
