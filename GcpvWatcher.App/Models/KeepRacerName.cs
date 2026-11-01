namespace GcpvWatcher.App.Models;

public sealed record KeepRacerName(string LastName, string FirstName, string Affiliation)
{
    public bool HasAny =>
        LastName.Length > 0 || FirstName.Length > 0 || Affiliation.Length > 0;

    /// <summary>
    /// Columns that follow the lane. Stops at the last field that has a value,
    /// so a row with no name data contributes nothing and a later field keeps its place.
    /// </summary>
    public IReadOnlyList<string> ColumnsToWrite()
    {
        var fields = new[] { LastName, FirstName, Affiliation };
        var lastUsed = -1;
        for (var i = 0; i < fields.Length; i++)
        {
            if (fields[i].Length > 0)
                lastUsed = i;
        }

        if (lastUsed < 0)
            return Array.Empty<string>();

        var columns = new string[lastUsed + 1];
        for (var i = 0; i <= lastUsed; i++)
            columns[i] = fields[i];
        return columns;
    }
}
