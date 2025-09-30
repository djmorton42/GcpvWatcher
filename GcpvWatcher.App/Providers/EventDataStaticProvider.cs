namespace GcpvWatcher.App.Providers;

public class EventDataStaticProvider : BaseEventDataProvider
{
    private readonly IEnumerable<string> _dataRows;

    public EventDataStaticProvider(IEnumerable<string> dataRows)
    {
        _dataRows = dataRows ?? throw new ArgumentNullException(nameof(dataRows));
    }

    public override Task<IEnumerable<string>> GetDataRowsAsync()
    {
        // Use base class filtering for comment lines
        var filteredRows = FilterCommentLines(_dataRows);
        return Task.FromResult(filteredRows);
    }
}
