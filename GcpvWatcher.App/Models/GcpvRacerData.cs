namespace GcpvWatcher.App.Models;

public record GcpvRacerData(
    string Lane,
    string Racer,
    string Affiliation
)
{
    public override string ToString()
    {
        return $"Lane {Lane}: {Racer} ({Affiliation})";
    }
}
