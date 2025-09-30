namespace GcpvWatcher.App.Models;

public class Racer
{
    public int RacerId { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;

    public Racer()
    {
    }

    public Racer(int racerId, string lastName, string firstName, string affiliation)
    {
        RacerId = racerId;
        LastName = lastName;
        FirstName = firstName;
        Affiliation = affiliation;
    }

    public override string ToString()
    {
        return $"{RacerId},{LastName},{FirstName},{Affiliation}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is Racer other)
        {
            return RacerId == other.RacerId &&
                   LastName == other.LastName &&
                   FirstName == other.FirstName &&
                   Affiliation == other.Affiliation;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RacerId, LastName, FirstName, Affiliation);
    }
}
