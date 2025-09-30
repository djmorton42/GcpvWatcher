namespace GcpvWatcher.App.Providers;

public abstract class BaseEventDataProvider : IEventDataProvider
{
    public abstract Task<IEnumerable<string>> GetDataRowsAsync();

    protected IEnumerable<string> FilterCommentLines(IEnumerable<string> lines)
    {
        return lines.Where(line => !string.IsNullOrWhiteSpace(line) && 
                                   !line.TrimStart().StartsWith(";") && 
                                   !line.TrimStart().StartsWith("#"));
    }
}
