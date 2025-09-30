namespace GcpvWatcher.App.Providers;

public abstract class BaseGcpvExportDataProvider : IGcpvExportDataProvider
{
    public abstract Task<IEnumerable<string>> GetDataRowsAsync();

    protected IEnumerable<string> FilterBlankLines(IEnumerable<string> lines)
    {
        return lines.Where(line => !string.IsNullOrWhiteSpace(line));
    }
}
