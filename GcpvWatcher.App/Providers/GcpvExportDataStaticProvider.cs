namespace GcpvWatcher.App.Providers;

public class GcpvExportDataStaticProvider : BaseGcpvExportDataProvider
{
    private readonly IEnumerable<string> _dataRows;

    public GcpvExportDataStaticProvider(IEnumerable<string> dataRows)
    {
        _dataRows = dataRows ?? throw new ArgumentNullException(nameof(dataRows));
    }

    public override Task<IEnumerable<string>> GetDataRowsAsync()
    {
        // Use base class filtering for blank lines only
        var filteredRows = FilterBlankLines(_dataRows);
        return Task.FromResult(filteredRows);
    }
}
