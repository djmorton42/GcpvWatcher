namespace GcpvWatcher.App.Models;

public record Racer(int RacerId, string LastName, string FirstName, string Affiliation)
{
    public override string ToString()
    {
        return $"{RacerId},{LastName},{FirstName},{Affiliation}";
    }
}
