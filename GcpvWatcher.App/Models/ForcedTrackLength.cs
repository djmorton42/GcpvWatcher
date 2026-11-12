namespace GcpvWatcher.App.Models;

/// <summary>
/// Operator choice for which track length to use when calculating laps.
/// Auto follows the file. 100m and 111m are used for every race.
/// </summary>
public enum ForcedTrackLength
{
    Auto,
    Meters100,
    Meters111
}
